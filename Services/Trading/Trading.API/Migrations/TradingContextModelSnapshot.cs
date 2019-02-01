﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trading.API.Data;

namespace Trading.API.Migrations
{
    [DbContext(typeof(TradingContext))]
    partial class TradingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.Property<decimal>("Dust");

                    b.Property<decimal>("EstimatedProfit");

                    b.Property<string>("InitialCurrency");

                    b.HasKey("Id");

                    b.ToTable("ArbitrageResults");
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
