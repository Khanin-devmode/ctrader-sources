using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]

    public class Zone_Recovery_Bot_Random_Entry : Robot
    {
        private const string label = "Zone recovery Bot";

        private RelativeStrengthIndex rsi;
        private DirectionalMovementSystem dms;

        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 0.5)]
        public double RewardRiskRatio { get; set; }

        [Parameter(DefaultValue = 1.5, MinValue = 1.2, MaxValue = 2, Step = 0.1)]
        public double HedgingRatio { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 5, MaxValue = 100, Step = 5)]
        public int RecoveryZonePips { get; set; }

        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }

        double stdLotSize;
        double upperZonePrice;
        double lowerZonePrice;
        double totalLongUnit = 0;
        double totalShortUnit = 0;
        double targetProfit = 0;
        Position[] allPosition = new Position[] { };
        
        
        bool longSignal = false;
        bool shortSignal = false;
        Random random = new Random();

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source, 14);
            dms = Indicators.DirectionalMovementSystem(14);
            
            //Get all open positions and resistance.
            allPosition = Positions.FindAll(label, SymbolName);
            
            
            if(allPosition.Length > 0){
                var entryPosition = allPosition[0]; //Double Check first position is the first entry.
                if(entryPosition.TradeType == TradeType.Buy){
                    upperZonePrice = entryPosition.EntryPrice;
                    lowerZonePrice = upperZonePrice - (RecoveryZonePips * Symbol.PipSize);
                }else if(entryPosition.TradeType == TradeType.Sell){
                    lowerZonePrice = entryPosition.EntryPrice;
                    upperZonePrice = lowerZonePrice + (RecoveryZonePips * Symbol.PipSize);
                }
                
                foreach (var position in allPosition){
                    if(position.TradeType == TradeType.Buy){
                        totalLongUnit += position.VolumeInUnits;
                    }else if(position.TradeType == TradeType.Sell){
                        totalShortUnit += position.VolumeInUnits;
                    }
                }
            
            }
            
        }

        protected override void OnTick()
        {
            RandomEntry();
            //when crossing the zone
            //crossing lower zone, short to with higher lot size.

            if (allPosition.Length > 0)
            {

                if (Symbol.Ask <= lowerZonePrice && totalLongUnit > totalShortUnit)
                {
                    double shortUnitInVolume = Symbol.NormalizeVolumeInUnits((totalLongUnit * HedgingRatio) - totalShortUnit, RoundingMode.Up);
                    var shortResult = ExecuteMarketOrder(TradeType.Sell, SymbolName, shortUnitInVolume, label);
                    if (shortResult.IsSuccessful)
                    {
                        //add total
                        totalShortUnit += shortResult.Position.VolumeInUnits;

                    }

                }
                else if (Symbol.Bid >= upperZonePrice && totalShortUnit > totalLongUnit)
                {
                    double longUnitInVolume = Symbol.NormalizeVolumeInUnits((totalShortUnit * HedgingRatio) - totalLongUnit, RoundingMode.Up);
                    var longResult = ExecuteMarketOrder(TradeType.Buy, SymbolName, longUnitInVolume, label);

                    if (longResult.IsSuccessful)
                    {
                        //add total
                        totalLongUnit += longResult.Position.VolumeInUnits;
                    }
                }

                if (Account.UnrealizedNetProfit > targetProfit)
                {

                    foreach (Position position in allPosition)
                    {
                        ClosePositionAsync(position);
                    }

                    Reset();

                }
                

            }




        }
        protected override void OnBar()
        {

            var highPrice = MarketSeries.High.LastValue;
            var openTime = MarketSeries.OpenTime.LastValue;
            var text = Chart.DrawText("text1", "High is here", openTime, highPrice, Color.Yellow);
            text.VerticalAlignment = VerticalAlignment.Bottom;
            text.HorizontalAlignment = HorizontalAlignment.Center;

            allPosition = Positions.FindAll(label, SymbolName);

            if(allPosition.Length == 0)
            {
                if (longSignal)
                {

                    stdLotSize = GetOptimalBuyUnit(RecoveryZonePips, StopLossPrc);
                    var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, stdLotSize, label);
                    if (result.IsSuccessful)
                    {
                        upperZonePrice = result.Position.EntryPrice;
                        lowerZonePrice = upperZonePrice - (RecoveryZonePips * Symbol.PipSize);
                        totalLongUnit += result.Position.VolumeInUnits;
                        targetProfit = Account.Equity * (StopLossPrc * RewardRiskRatio);

                    }

                }
                else if (shortSignal)
                {

                    stdLotSize = GetOptimalBuyUnit(RecoveryZonePips, StopLossPrc);
                    var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, stdLotSize, label);
                    if (result.IsSuccessful)
                    {
                        lowerZonePrice = result.Position.EntryPrice;
                        upperZonePrice = lowerZonePrice + (RecoveryZonePips * Symbol.PipSize);
                        totalShortUnit += result.Position.VolumeInUnits;
                        targetProfit = Account.Equity * (StopLossPrc * RewardRiskRatio);
                    }

                }

            }

        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        private void RandomEntry(){
             if(random.Next(2) == 0){
                longSignal = true;
             }else{
                shortSignal = true;
             }
             //random.Next(2) == 0 ? TradeType.Buy : TradeType.Sell;
             
        }

        protected double GetOptimalBuyUnit(int stopLossPips, double stopLossPrc)
        {

            double accEquity;
            double stopLossAsset;
            double stopLossQuote;
            double slChartSize;
            double optimalLotSizeInUnit;

            accEquity = Account.Equity;
            stopLossAsset = accEquity * stopLossPrc;
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well       
            slChartSize = stopLossPips * Symbol.PipSize;
            optimalLotSizeInUnit = stopLossQuote / slChartSize;

            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);

        }

        private void Reset()
        {
            stdLotSize = 0;
            upperZonePrice = 0;
            lowerZonePrice = 0;
            totalLongUnit = 0;
            totalShortUnit = 0;
            targetProfit = 0;
            shortSignal = false;
            longSignal = false;
            allPosition = new Position[] { };
        }
    }
}