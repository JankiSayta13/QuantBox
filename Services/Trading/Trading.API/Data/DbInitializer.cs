﻿using ExchangeManager.Models;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trading.API.Domain;

namespace Trading.API.Data
{
    public static class DbInitializer
    {
        public static void Seed(TradingContext context)
        {
            if (!context.Exchanges.Any())
            {
                context.Exchanges.AddRange(new List<Exchange>()
                {
                    new Exchange(){
                        Name = "Binance",
                        TradingEnabled = true
                    },
                    new Exchange(){
                        Name = "BtcMarkets",
                        TradingEnabled = true
                    },
                    new Exchange(){
                        Name = "KuCoin",
                        TradingEnabled = true
                    },
                    new Exchange(){
                        Name = "Coinjar",
                        TradingEnabled = true
                    },
                });
                context.SaveChanges();
            }

            if (!context.Bots.Any())
            {
                context.Bots.AddRange(new List<Bot>()
                {
                    new Bot()
                    {
                        Name = "Triangle Arbitrage",
                        TradingEnabled = false,
                        Accounts = new List<ExchangeConfig>()
                    },
                    new Bot()
                    {
                        Name = "Normal Arbitrage",
                        TradingEnabled = false,
                        Accounts = new List<ExchangeConfig>()
                    }
                });
                context.SaveChanges();
            }
        }
    }
}
