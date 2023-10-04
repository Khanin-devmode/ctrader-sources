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
    public class NewcBot : Robot
    {
        
        private const string Label = "Simple Bars Breakout";
        
        [Parameter(DefaultValue = 20, MinValue = 5, MaxValue = 100, Step = 5)]
        public int SlPips { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double SlPrc { get; set; }
        
        [Parameter(DefaultValue = 1.5, MinValue = 1.0, MaxValue = 2, Step = 0.1)]
        public double RiskRewardRatio { get; set; }
        
        [Parameter(DefaultValue = 30, MinValue = 10, MaxValue = 90, Step = 5)]
        public int BackwardBars { get; set; }
        
        [Parameter(DefaultValue = false)]
        public bool IsTrailing {get;set;}
        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            
        }

        protected override void OnTick()
        {
            // Handle price updates here
        }
        
        protected override void OnBar()
        {
            var longPosition = Positions.Find(Label,SymbolName,TradeType.Buy);
            var shortPosition = Positions.Find(Label,SymbolName,TradeType.Sell);
            
            var optimalBuyUnit = GetOptimalBuyUnit(SlPips,SlPrc);
            
            
            if(LongSignal() && longPosition == null){
            
                var result = ExecuteMarketOrder(TradeType.Buy,SymbolName,optimalBuyUnit,Label,SlPips,SlPips*RiskRewardRatio,"",IsTrailing);
            
            } 
            
            if(ShortSignal() && shortPosition  == null){
               
               var result = ExecuteMarketOrder(TradeType.Sell,SymbolName,optimalBuyUnit,Label,SlPips,SlPips*RiskRewardRatio,"",IsTrailing);
            }
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        private bool LongSignal()
        {
            for(var i = 2 ; i <= BackwardBars; i++ ){
                if(!(Bars.Last(1).High > Bars.Last(i).High)){
                       
                    
                    return false;
                }
               
            }
            
             return true;
        }
        
        private bool ShortSignal()
        {
            for(var i = 2 ; i <= BackwardBars; i++ ){
                if(!(Bars.Last(1).Low < Bars.Last(i).Low)){
                        return false;
                }

                
            }
            
            return true; 
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