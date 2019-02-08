﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trading.API.Data;

namespace Trading.API.Migrations
{
    [DbContext(typeof(TradingContext))]
    [Migration("20190208141055_ChangeTimeTakenToStartEndTimes")]
    partial class ChangeTimeTakenToStartEndTimes
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ExchangeManager.Models.ExchangeConfig", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BotId");

                    b.Property<string>("Name");

                    b.Property<string>("Nickname");

                    b.Property<string>("PrivateKey");

                    b.Property<string>("PublicKey");

                    b.Property<bool>("Simulated");

                    b.HasKey("Id");

                    b.HasIndex("BotId");

                    b.ToTable("ExchangeCredentials");
                });

            modelBuilder.Entity("ExchangeManager.Models.TradeResult", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("Amount");

                    b.Property<decimal>("AmountFilled");

                    b.Property<string>("ArbitrageTradeResultsId");

                    b.Property<decimal>("AveragePrice");

                    b.Property<string>("CorrelationId");

                    b.Property<decimal>("Fees");

                    b.Property<string>("FeesCurrency");

                    b.Property<DateTime>("FillDate");

                    b.Property<string>("MarketSymbol");

                    b.Property<string>("Message");

                    b.Property<DateTime>("OrderDate");

                    b.Property<string>("OrderId");

                    b.Property<int>("OrderSide");

                    b.Property<decimal>("Price");

                    b.Property<int>("Result");

                    b.Property<string>("TradeId");

                    b.HasKey("Id");

                    b.HasIndex("ArbitrageTradeResultsId");

                    b.ToTable("TradeResult");
                });

            modelBuilder.Entity("Trading.API.Domain.ArbitrageTradeResults", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("ActualProfit");

                    b.Property<string>("BotId");

                    b.Property<decimal>("Dust");

                    b.Property<decimal>("EstimatedProfit");

                    b.Property<string>("InitialCurrency");

                    b.Property<DateTime>("TimeFinished");

                    b.Property<DateTime>("TimeStarted");

                    b.HasKey("Id");

                    b.ToTable("ArbitrageResults");
                });

            modelBuilder.Entity("Trading.API.Domain.Bot", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<bool>("TradingEnabled");

                    b.HasKey("Id");

                    b.ToTable("Bots");
                });

            modelBuilder.Entity("Trading.API.Domain.Exchange", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<bool>("TradingEnabled");

                    b.HasKey("Id");

                    b.ToTable("Exchanges");
                });

            modelBuilder.Entity("ExchangeManager.Models.ExchangeConfig", b =>
                {
                    b.HasOne("Trading.API.Domain.Bot")
                        .WithMany("Accounts")
                        .HasForeignKey("BotId");
                });

            modelBuilder.Entity("ExchangeManager.Models.TradeResult", b =>
                {
                    b.HasOne("Trading.API.Domain.ArbitrageTradeResults")
                        .WithMany("Trades")
                        .HasForeignKey("ArbitrageTradeResultsId");
                });
#pragma warning restore 612, 618
        }
    }
}
