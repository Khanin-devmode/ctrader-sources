using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Threading.Tasks;

// Entry: Long when close is higher of BB top 
// & Close is the highest of the last x bar
// & ADX is higher than 25
// & stop loss pips is higher than minimum stop lost to reduce noise. 
// Entry volume with optimal unit at 2% SL of equity.
// Short entry is vice versa.
// Exit: Fixed SL

// REMINDER: This bot cannot utilise 100% cpu, not sure why.

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class BBBreakOut_a : Robot
    {

        private BollingerBands bb;
        private DirectionalMovementSystem dms;
        
        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 2, MinValue = 2, MaxValue = 4, Step = 1)]
        public int StdDiv { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 10, MaxValue = 60, Step = 5)]
        public int BackwardBars { get; set; }

        [Parameter(DefaultValue = 0.02, MinValue = 0.01, MaxValue = 0.05, Step = 0.01)]
        public double StopLossPrc { get; set; }

        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 3, Step = 0.2)]
        public double TpRatio { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 50, Step = 5)]
        public int SlPips { get; set; }

        private const string label = "BB Breakout version A bot";

        protected DataSeries Source;
        protected int adxThres = 25;


        protected override void OnStart()
        {

            bb = Indicators.BollingerBands(Source, Period, 2, MovingAverageType.Simple);
            dms = Indicators.DirectionalMovementSystem(Period);

        }

        protected override void OnTick()
        {
            // Handle price updates here

        }

        protected override void OnBar()
        {
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);


            if (LongSignal() && longPosition == null)
            {

               
                int tpPips = Convert.ToInt16(SlPips * TpRatio);



                    var volumeInUnits = GetOptimalBuyUnit(SlPips, StopLossPrc);
                    ExecuteMarketOrderAsync(TradeType.Buy, SymbolName, volumeInUnits, label, SlPips, tpPips, "", true);
                



            }
            else if (ShortSignal() && shortPosition == null)
            {


                int tpPips = Convert.ToInt16(SlPips * TpRatio);


                    var volumeInUnits = GetOptimalBuyUnit(SlPips, StopLossPrc);
                    ExecuteMarketOrderAsync(TradeType.Sell, SymbolName, volumeInUnits, label, SlPips, tpPips, "", true);
                

            }

        }


        private bool LongSignal()
        {
            var top = bb.Top.Last(1);
            bool isBreakingBB = Bars.Last(1).Close > top;

            return isBreakingBB && IsBreakingResistance() && IsStrongTrend();
        }

        private bool ShortSignal()
        {
            var bottom = bb.Bottom.Last(1);
            bool isBreakingBB = Bars.Last(1).Close < bottom;

            return isBreakingBB && IsBreakingSupport() && IsStrongTrend();
        }

        private bool IsBreakingResistance()
        {
            for (var i = 2; i <= BackwardBars; i++)
            {
                if (!(Bars.Last(1).High > Bars.Last(i).High))
                {

                    return false;
                }

            }

            return true;
        }

        private bool IsBreakingSupport()
        {

            for (var i = 2; i <= BackwardBars; i++){
                if (!(Bars.Last(1).Low < Bars.Last(i).Low))
                {
                    return false;
                }
            }

            return true;
        }
        
        private bool IsStrongTrend(){
            return dms.ADX.LastValue > adxThres;
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

    }


}