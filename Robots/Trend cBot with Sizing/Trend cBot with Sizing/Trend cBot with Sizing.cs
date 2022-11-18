// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Automate API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//    
//    All changes to this file might be lost on the next application update.
//    If you are going to modify this file please make a copy using the "Duplicate" command.
//
//    The "Sample Trend cBot" will buy when fast period moving average crosses the slow period moving average and sell when 
//    the fast period moving average crosses the slow period moving average. The orders are closed when an opposite signal 
//    is generated. There can only by one Buy or Sell order at any time.
//
// -------------------------------------------------------------------------------------------------

using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TrendcBotwithSizing : Robot
    {

        [Parameter("MA Type", Group = "Moving Average")]
        public MovingAverageType MAType { get; set; }

        [Parameter("Source", Group = "Moving Average")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Slow Periods", Group = "Moving Average", DefaultValue = 10)]
        public int SlowPeriods { get; set; }

        [Parameter("Fast Periods", Group = "Moving Average", DefaultValue = 5)]
        public int FastPeriods { get; set; }
        
        [Parameter(DefaultValue = 20)]
        public int StopLossPips { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }

        private MovingAverage slowMa;
        private MovingAverage fastMa;
        private const string label = "Trend cBot with Sizing";

        protected override void OnStart()
        {
            fastMa = Indicators.MovingAverage(SourceSeries, FastPeriods, MAType);
            slowMa = Indicators.MovingAverage(SourceSeries, SlowPeriods, MAType);
        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);

            var currentSlowMa = slowMa.Result.Last(0);
            var currentFastMa = fastMa.Result.Last(0);
            var previousSlowMa = slowMa.Result.Last(1);
            var previousFastMa = fastMa.Result.Last(1);

            if (previousSlowMa > previousFastMa && currentSlowMa <= currentFastMa && longPosition == null)
            {
                if (shortPosition != null)
                    ClosePosition(shortPosition);

                ExecuteMarketOrder(TradeType.Buy, SymbolName, GetOptimalBuyUnit(StopLossPips,StopLossPrc), label, StopLossPips, null);
            }
            else if (previousSlowMa < previousFastMa && currentSlowMa >= currentFastMa && shortPosition == null)
            {
                if (longPosition != null)
                    ClosePosition(longPosition);

                ExecuteMarketOrder(TradeType.Sell, SymbolName, GetOptimalBuyUnit(StopLossPips,StopLossPrc), label, StopLossPips, null);
            }
        }
        
        protected double GetOptimalBuyUnit(int stopLossPips, double stopLossPrc)
        {
        
            double accEquity;
            double stopLossAsset;
            double stopLossQuote;
            double slChartSize;
            double optimalLotSizeInUnit;
                
            accEquity = Account.Equity;
            Print("Account Equity: " + accEquity); 
            
            stopLossAsset = accEquity*stopLossPrc;
            
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well

            slChartSize = stopLossPips * Symbol.PipSize;

            optimalLotSizeInUnit = stopLossQuote/slChartSize;
            
            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);
            
        }
    }
}
