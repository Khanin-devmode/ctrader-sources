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
    public class BBADXbot : Robot
    {

        private BollingerBands bb;
        private DirectionalMovementSystem dms;
        
        //[Parameter(DefaultValue = 14)]
        //public int BBPeriod { get; set; }   

        //[Parameter(DefaultValue = 14)]
        //public int AdxPeriod { get; set; }
        
        [Parameter(DefaultValue = 25)]
        public int AdxThres { get; set; }
        
        [Parameter(DefaultValue = 14)]
        public int IndicatorPeriod { get; set; }
        
        [Parameter(DefaultValue = 2)]
        public int StdDiv { get; set; }
        
        [Parameter(DefaultValue = 20)]
        public int StopLossPips { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }
        
        [Parameter(DefaultValue = true)]
        public bool UseAdx { get; set; }
        
        private const string label = "BB ADX bot";
        
        protected DataSeries Source;

        protected override void OnStart()
        {
            
            bb = Indicators.BollingerBands(Source,IndicatorPeriod,2,MovingAverageType.Simple);
            dms = Indicators.DirectionalMovementSystem(IndicatorPeriod);
            
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
            var mid = bb.Main.Last(1);

            var volumeInUnits = GetOptimalBuyUnit(StopLossPips,StopLossPrc);
            
            if(UseAdx)
            {
                if(dms.ADX.LastValue <= AdxThres)
                {   
                    if (Bars.LowPrices.Last(1) <= bottom && longPosition == null )
                    {
                        ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, StopLossPips, null);
                    }
                    else if (Bars.HighPrices.Last(1) >= top && shortPosition == null)
                    {
                        ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, StopLossPips, null);
                    }
                }
                
            }else {
            
                    if (Bars.LowPrices.Last(1) <= bottom && longPosition == null )
                    {
                        ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, StopLossPips, null);
                    }
                    else if (Bars.HighPrices.Last(1) >= top && shortPosition == null)
                    {
                        ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, StopLossPips, null);
                    }
            }
               
            if(Bars.ClosePrices.Last(1) >= mid && longPosition != null){
                ClosePosition(longPosition);
            }else if(Bars.ClosePrices.Last(1) <= mid && shortPosition != null){
                ClosePosition(shortPosition);
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