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
    public class BB_RSI_bot : Robot
    {

        private BollingerBands bb;
        private RelativeStrengthIndex rsi;
        
        [Parameter(DefaultValue = 70, MinValue = 65, MaxValue = 75, Step = 5)]
        public int RsiHigh { get; set; }
        
        [Parameter(DefaultValue = 30, MinValue = 25, MaxValue = 35, Step = 5)]
        public int RsiLow { get; set; }
        
        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2)]
        public int RsiPeriod { get; set; }
        
        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2)]
        public int BbPeriod { get; set; }
        
        [Parameter(DefaultValue = 2, MinValue = 2, MaxValue = 4, Step = 1)]
        public int StdDiv { get; set; }
        
        [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 50, Step = 5)]
        public int StopLossPips { get; set; }
        
        [Parameter(DefaultValue = 0.02, MinValue = 0.01, MaxValue = 0.05, Step = 0.01)]
        public double StopLossPrc { get; set; }
        
        private const string label = "BB RSI bot";
        
        protected DataSeries Source;

        protected override void OnStart()
        {
            
            bb = Indicators.BollingerBands(Source,BbPeriod,2,MovingAverageType.Simple);
            rsi = Indicators.RelativeStrengthIndex(Source,RsiPeriod);
            
        }

        protected override void OnTick()
        {
            // Handle price updates here
            
        }
        
        protected override void OnBar()
        {
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);
        
            var top = bb.Top.Last(1);
            var bottom = bb.Bottom.Last(1);
            var lastRsi = rsi.Result.Last(1);


            var volumeInUnits = GetOptimalBuyUnit(StopLossPips,StopLossPrc);

            
            if (Bars.LowPrices.Last(1) <= bottom && lastRsi >= RsiLow && rsi.Result.IsRising())
            {
                if(shortPosition != null){
                    ClosePosition(shortPosition);
                }else if(longPosition == null){
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, StopLossPips, null);
                }
                
            } else if (Bars.HighPrices.Last(1) >= top && lastRsi <= RsiHigh && rsi.Result.IsFalling())
            {
                if(longPosition != null){
                    ClosePosition(longPosition);
                }else if(shortPosition == null){
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, StopLossPips, null);
                }
                
            }
        
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        protected double GetOptimalBuyUnit(int stopLossPips, double stopLossPrc)
        {
        
            double accEquity;
            double stopLossAsset;
            double stopLossQuote;
            double slChartSize;
            double optimalLotSizeInUnit;
                
            accEquity = Account.Equity;
            stopLossAsset = accEquity*stopLossPrc;
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well
            slChartSize = stopLossPips * Symbol.PipSize;
            optimalLotSizeInUnit = stopLossQuote/slChartSize;
  
            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);
            
        }
        
    }
    
    
}