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

    public class Zone_Recovery_Bot : Robot
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

        [Parameter(DefaultValue = 25, MinValue = 10, MaxValue = 100, Step = 5)]
        public int AdxThres { get; set; }

        double stdLotSize;
        double upperZonePrice;
        double lowerZonePrice;
        double totalLongUnit = 0;
        double totalShortUnit = 0;
        double targetProfit = 0;
        Position[] allPosition = new Position[] { };

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source, 14);
            dms = Indicators.DirectionalMovementSystem(14);
        }

        protected override void OnTick()
        {

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

            allPosition = Positions.FindAll(label, SymbolName);

            if(allPosition.Length == 0)
            {
                if (LongSignal())
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
                else if (ShortSignal())
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

        private bool LongSignal()
        {

            return dms.ADX.Last(1) >= AdxThres && rsi.Result.Last(1) > 70;

            //return rsi.Result.Last(1) > 30 && rsi.Result.Last(2) < 30; //Sample signal
        }

        private bool ShortSignal()
        {
            return dms.ADX.Last(1) >= AdxThres && rsi.Result.Last(1) < 30;
            // return rsi.Result.Last(1) < 70 && rsi.Result.Last(2) > 70; //Sample signal
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
            allPosition = new Position[] { };
        }
    }
}