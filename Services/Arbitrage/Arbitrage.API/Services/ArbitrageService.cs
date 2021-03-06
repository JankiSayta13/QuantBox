﻿using Arbitrage.API.IntegrationEvents.Events;
using Arbitrage.Domain;
using BuildingBlocks.EventBus.Abstractions;
using ExchangeManager.Clients;
using ExchangeManager.Helpers;
using ExchangeManager.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arbitrage.Api.Services
{
    public class ArbitrageService : BackgroundService
    {
        private readonly IEventBus _eventBus;
        //private readonly IHubContext<ArbitrageHub, IArbitrageHub> _arbitrageHub;

        public readonly Dictionary<string, ArbitrageResult> triangleResults = new Dictionary<string, ArbitrageResult>();
        public readonly Dictionary<string, ArbitrageResult> normalResults = new Dictionary<string, ArbitrageResult>();

        public ArbitrageResult bestTriangleProfit = new ArbitrageResult() { Profit = -101 };
        public ArbitrageResult worstTriangleProfit = new ArbitrageResult() { Profit = 101 };
        public ArbitrageResult bestNormalProfit = new ArbitrageResult() { Profit = -101 };
        public ArbitrageResult worstNormalProfit = new ArbitrageResult() { Profit = 101 };

        private readonly List<IExchange> _exchanges = new List<IExchange>()
        {
            new Binance(),
            new KuCoin(),
            new BtcMarkets(),
            new Coinjar()
        };

        public ArbitrageService()
        {
        }

        public ArbitrageService(IEventBus eventBus/*, IHubContext<ArbitrageHub, IArbitrageHub> arbitrageHub*/)
        {
            _eventBus = eventBus;
            //_arbitrageHub = arbitrageHub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var exchange in _exchanges)
            {
                exchange.StartOrderbookListener();
            }

            Task.Run(() =>
                StartTriangleArbitrageListener()
            );

            Task.Run(() =>
                StartNormalArbitrageListener()
            );
            
            return Task.CompletedTask;
        }

        //Calculate arb chances for triangle arb and pass it to the UI (?via SignalR?)
        public Task StartTriangleArbitrageListener()
        {
            while (true)
            {
                try
                {
                    foreach (var exchange in _exchanges)
                    {
                        CheckExchangeForTriangleArbitrage(exchange);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
        }

        public void CheckExchangeForTriangleArbitrage(IExchange exchange)
        {
            try {
                decimal altAmount, alt2Amount, finalAmount, baseAmount;
                //Assuming we are testing BTC/ETH->ETH/XRP->XRP/BTC, startCurrency == BTC, middleCurrency == ETH, endCurrency == XRP
                string startCurrency, middleCurrency, endCurrency; //The currency that's bought/sold from in the first transaction
                var audInvested = 100;
                var orderbooks = exchange.Orderbooks;

                foreach (var market in orderbooks.Values)
                {
                    //Loop every market with a matching currency except itself (this could start on the base or alt currency)
                    foreach (var market2 in orderbooks.Values.Where(x => x.Pair != market.Pair && (x.AltCurrency == market.AltCurrency || x.BaseCurrency == market.AltCurrency || x.AltCurrency == market.BaseCurrency || x.BaseCurrency == market.BaseCurrency)))
                    {
                        //If the base/alt currency for the next market is the base currency, we need to bid (i.e. Buy for first trade)
                        if (market.BaseCurrency == market2.BaseCurrency || market.BaseCurrency == market2.AltCurrency)
                        {
                            baseAmount = ConvertAudToCrypto(orderbooks, market.AltCurrency, audInvested);

                            if(baseAmount == 0)
                            {
                                continue; //Asset prices not loaded yet
                            }

                            try
                            {
                                var bids = orderbooks[market.AltCurrency + "/" + market.BaseCurrency].Bids;
                                if(bids.Count() == 0)
                                {
                                    continue;
                                }
                                altAmount = baseAmount * PriceCalculator.GetPriceQuote(bids, baseAmount);
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                            startCurrency = market.AltCurrency;
                            middleCurrency = market.BaseCurrency;
                        }
                        else //Else we need to ask (i.e. Sell for first trade)
                        {
                            baseAmount = ConvertAudToCrypto(orderbooks, market.BaseCurrency, audInvested);
                            if (baseAmount == 0)
                            {
                                continue; //Asset prices not loaded yet
                            }

                            try
                            {
                                var asks = orderbooks[market.AltCurrency + "/" + market.BaseCurrency].Asks;
                                if (asks.Count() == 0)
                                {
                                    continue; //Prices not loaded yet
                                }
                                altAmount = baseAmount / PriceCalculator.GetPriceQuote(asks, PriceCalculator.ConvertBaseToAlt(asks.First().Price, baseAmount)); //~3000 ETH from 100 BTC
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                            startCurrency = market.BaseCurrency;
                            middleCurrency = market.AltCurrency;
                        }

                        //If the alt bought in step 1 is now a base, use ask price
                        if (market2.BaseCurrency == middleCurrency)
                        {
                            endCurrency = market2.AltCurrency;
                            try
                            {
                                var asks = orderbooks[market2.AltCurrency + "/" + market2.BaseCurrency].Asks;
                                if (asks.Count() == 0)
                                {
                                    continue; //Prices not loaded yet
                                }
                                alt2Amount = altAmount / PriceCalculator.GetPriceQuote(asks, PriceCalculator.ConvertBaseToAlt(asks.First().Price, altAmount));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                        }
                        else //Otherwise it's the alt currency (i.e. we're selling to the new coin)
                        {
                            endCurrency = market2.BaseCurrency;
                            try
                            {
                                var bids = orderbooks[market2.AltCurrency + "/" + market2.BaseCurrency].Bids;
                                if(bids.Count() == 0)
                                {
                                    continue; //Not loaded yet
                                }
                                alt2Amount = altAmount * PriceCalculator.GetPriceQuote(bids, altAmount);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                        }
                        //Find the final market (i.e. the market that has the middle and end currencies)
                        if (!orderbooks.TryGetValue(startCurrency + "/" + endCurrency, out Orderbook finalMarket))
                        {
                            orderbooks.TryGetValue(endCurrency + "/" + startCurrency, out finalMarket);
                        }

                        //If null, there's no pairs to finish the arb
                        if (finalMarket == null)
                        {
                            continue;
                        }

                        //If the base currency is the first currency, we need to sell (i.e. use bid)
                        if(finalMarket.BaseCurrency == startCurrency)
                        {
                            try
                            {
                                var bids = orderbooks[finalMarket.AltCurrency + "/" + finalMarket.BaseCurrency].Bids;
                                if(bids.Count() == 0)
                                {
                                    continue; //Not loaded yet
                                }
                                finalAmount = alt2Amount * PriceCalculator.GetPriceQuote(bids, alt2Amount);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                        }
                        else //Else we buy (i.e. use ask)
                        {
                            try
                            {
                                var asks = orderbooks[finalMarket.AltCurrency + "/" + finalMarket.BaseCurrency].Asks;
                                if (asks.Count() == 0)
                                {
                                    continue; //Prices not loaded yet
                                }
                                finalAmount = alt2Amount / PriceCalculator.GetPriceQuote(asks, PriceCalculator.ConvertBaseToAlt(asks.First().Price, alt2Amount));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                continue;
                            }
                        }

                        decimal percentProfit = (finalAmount - baseAmount) / baseAmount * 100;
                        var result = new ArbitrageResult()
                        {
                            Exchanges = new List<string>() { exchange.Name },
                            Pairs = new List<Pair>() {
                                new Pair(market.Pair, market.BaseCurrency, market.AltCurrency),
                                new Pair(market2.Pair, market2.BaseCurrency, market2.AltCurrency),
                                new Pair(finalMarket.Pair, finalMarket.BaseCurrency, finalMarket.AltCurrency)
                            },
                            Profit = percentProfit,
                            TransactionFee = exchange.Fee * 3,
                            InitialCurrency = startCurrency,
                            InitialLiquidity = baseAmount,
                            Type = ArbitrageType.Triangle
                        };
                        StoreTriangleResults(result);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //Calculate arb chances for normal arb
        public Task StartNormalArbitrageListener()
        {
            while (true)
            {
                try
                {
                    foreach (var exchange in _exchanges)
                    {
                        CheckExchangeForNormalArbitrage(exchange);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
        }

        public void CheckExchangeForNormalArbitrage(IExchange startExchange)
        {
            var orderbooks = startExchange.Orderbooks;
            decimal endAmountBought;
            decimal audInvested = 100;

            try
            {
                foreach (var startOrderbook in orderbooks.Values) {
                    foreach (var exchange in _exchanges.Where(x => x.Name != startExchange.Name))
                    {
                        //Base->Alt->Base
                        var baseAmount = ConvertAudToCrypto(orderbooks, startOrderbook.BaseCurrency, audInvested);
                        if (baseAmount == 0)
                        {
                            continue; //Asset prices not loaded yet
                        }

                        if(!exchange.Orderbooks.TryGetValue(startOrderbook.AltCurrency + "/" + startOrderbook.BaseCurrency, out Orderbook endOrderbook))
                        {
                            exchange.Orderbooks.TryGetValue(startOrderbook.BaseCurrency + "/" + startOrderbook.AltCurrency, out endOrderbook);
                        }
                        if (endOrderbook == null)
                        {
                            continue; //Other exchange doesn't have the pair
                        }

                        var asks = startOrderbook.Asks;
                        if (asks.Count() == 0)
                        {
                            continue;
                        }
                        var startAmountBought = baseAmount / PriceCalculator.GetPriceQuote(asks, PriceCalculator.ConvertBaseToAlt(asks.First().Price, baseAmount));

                        if (endOrderbook.BaseCurrency == startOrderbook.BaseCurrency)
                        {
                            var endBids = endOrderbook.Bids;
                            if (endBids.Count() == 0)
                            {
                                continue;
                            }
                            endAmountBought = startAmountBought * PriceCalculator.GetPriceQuote(endBids, startAmountBought);
                        }
                        else
                        {
                            var endAsks = endOrderbook.Asks;
                            if (endAsks.Count() == 0)
                            {
                                continue;
                            }
                            endAmountBought = startAmountBought / PriceCalculator.GetPriceQuote(endAsks, PriceCalculator.ConvertBaseToAlt(endAsks.First().Price, startAmountBought));
                        }

                        decimal percentProfit = (endAmountBought - baseAmount) / baseAmount * 100;

                        var result = new ArbitrageResult()
                        {
                            Exchanges = new List<string>() { startExchange.Name, exchange.Name },
                            Pairs = new List<Pair>() { new Pair(startOrderbook.Pair, startOrderbook.BaseCurrency, startOrderbook.AltCurrency) },
                            Profit = percentProfit,
                            TransactionFee = startExchange.Fee + exchange.Fee,
                            InitialCurrency = startOrderbook.BaseCurrency,
                            InitialLiquidity = baseAmount,
                            Type = ArbitrageType.Normal
                        };
                        StoreNormalResults(result);

                        //Alt->Base->Alt
                        baseAmount = ConvertAudToCrypto(orderbooks, startOrderbook.AltCurrency, audInvested);
                        if (baseAmount == 0)
                        {
                            continue; //Asset prices not loaded yet
                        }

                        var bids = startOrderbook.Bids;
                        if (bids.Count() == 0)
                        {
                            continue;
                        }
                        startAmountBought = baseAmount * PriceCalculator.GetPriceQuote(bids, baseAmount);

                        if (endOrderbook.BaseCurrency == startOrderbook.BaseCurrency)
                        {
                            var endAsks = endOrderbook.Asks;
                            if (endAsks.Count() == 0)
                            {
                                continue;
                            }
                            endAmountBought = startAmountBought / PriceCalculator.GetPriceQuote(endAsks, PriceCalculator.ConvertBaseToAlt(endAsks.First().Price, startAmountBought));
                        }
                        else
                        {
                            var endBids = endOrderbook.Bids;
                            if (endBids.Count() == 0)
                            {
                                continue;
                            }
                            endAmountBought = startAmountBought * PriceCalculator.GetPriceQuote(endBids, startAmountBought);
                        }

                        percentProfit = (endAmountBought - baseAmount) / baseAmount * 100;

                        result = new ArbitrageResult()
                        {
                            Exchanges = new List<string>() { startExchange.Name, exchange.Name },
                            Pairs = new List<Pair>() { new Pair(startOrderbook.Pair, startOrderbook.AltCurrency, startOrderbook.BaseCurrency) },
                            Profit = percentProfit,
                            TransactionFee = startExchange.Fee + exchange.Fee,
                            InitialCurrency = startOrderbook.AltCurrency,
                            InitialLiquidity = baseAmount,
                            Type = ArbitrageType.Normal
                        };
                        StoreNormalResults(result);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error in CheckExchangeForNormalArbitrage (" + e.Message + ")");
            }
        }

        public void StoreTriangleResults(ArbitrageResult result)
        {
            //Generate a result key so we can match the same results later with an O(1) lookup
            string resultKey = string.Join(',', result.Exchanges) + '-' + string.Join(',', result.Pairs.Select(x => x.AltCurrency + "/" + x.BaseCurrency));

            if (bestTriangleProfit.Profit < result.Profit)
            {
                bestTriangleProfit = result;
            }
            if (worstTriangleProfit.Profit > result.Profit)
            {
                worstTriangleProfit = result;
            }

            if (!triangleResults.TryGetValue(resultKey, out ArbitrageResult currentResult))
            {
                triangleResults.Add(resultKey, result);
            }
            else
            {
                currentResult = result;
            }

            if(result.Profit > result.TransactionFee)
            {
                var @newTradeEvent = new ArbitrageFoundIntegrationEvent(result);
                _eventBus.Publish(@newTradeEvent);
            }
        }

        public void StoreNormalResults(ArbitrageResult result)
        {
            //Generate a result key so we can match the same results later with an O(1) lookup
            string resultKey = string.Join(',', result.Exchanges) + '-' + string.Join(',', result.Pairs.Select(x => x.AltCurrency + "/" + x.BaseCurrency));

            if (bestNormalProfit.Profit < result.Profit)
            {
                bestNormalProfit = result;
            }
            if (worstNormalProfit.Profit > result.Profit)
            {
                worstNormalProfit = result;
            }

            if (!normalResults.TryGetValue(resultKey, out ArbitrageResult currentResult))
            {
                normalResults.Add(resultKey, result);
            }
            else
            {
                currentResult = result;
            }

            if (result.Profit > result.TransactionFee)
            {
                var @newTradeEvent = new ArbitrageFoundIntegrationEvent(result);
                _eventBus.Publish(@newTradeEvent);
            }
        }

        //Converts AUD to crypto at the market rate
        public decimal ConvertAudToCrypto(Dictionary<string, Orderbook> orderbooks, string asset, decimal audAmount)
        {
            try
            {
                decimal btcAudPrice = _exchanges.First(x => x.Name == "BtcMarkets").Orderbooks["BTC/AUD"].Asks.First().Price; //Use BtcMarkets as BTC/AUD price reference since they have the most volume
                decimal btcFromAud = audAmount / btcAudPrice;

                if (asset == "BTC")
                {
                    return btcFromAud;
                }

                if (!orderbooks.TryGetValue(asset + "/BTC", out Orderbook btcAsset))
                {
                    orderbooks.TryGetValue("BTC/" + asset, out btcAsset);
                }
                if (btcAsset == null || btcAsset.Asks.Count() == 0)
                {
                    return 0; //Not populated yet
                }

                decimal assetFromBtc;
                if (btcAsset.AltCurrency == "BTC")
                {
                    decimal btcAssetPrice = btcAsset.Bids.First().Price;
                    assetFromBtc = btcFromAud * btcAssetPrice;
                }
                else
                {
                    decimal btcAssetPrice = btcAsset.Asks.First().Price;
                    assetFromBtc = btcFromAud / btcAssetPrice;
                }

                return assetFromBtc;
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong in ConvertAudToCrypto (" + e.Message + ")");
                return 0;
            }
        }
    }
}
