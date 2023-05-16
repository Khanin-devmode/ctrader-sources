using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

//Simple breakout with sl as low from 
//Entry: if close is higher than high of last x bar.
//Exit: Simple TP and SL. SL at lowest before breakout.

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
    public class NewcBot : Robot
    {
        
        private const string Label = "Simple Bars Breakout";
        
        [Parameter(DefaultValue = 0.02)]
        public double SlPrc { get; set; }
        
        [Parameter(DefaultValue = 1.5, MinValue = 1.0, MaxValue = 2, Step = 0.1)]
        public double RiskRewardRatio { get; set; }
        
        [Parameter(DefaultValue = 30, MinValue = 10, MaxValue = 90, Step = 5)]
        public int BackwardBars { get; set; }

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
            
                var result = ExecuteMarketOrder(TradeType.Buy,SymbolName,optimalBuyUnit,Label,SlPips,SlPips*RiskRewardRatio);
            
            } 
            
            if(ShortSignal() && shortPosition  == null){
               
               var result = ExecuteMarketOrder(TradeType.Sell,SymbolName,optimalBuyUnit,Label,SlPips,SlPips*RiskRewardRatio);
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
        
        private double GetHighLastXBars(int x){
        
            List<double> highPrices = new List<double>();
            
            for( int i =2; i <= x; i++){
                highPrices.Add(Bars.Last(i).High);
            }
            
            return highPrices.Max();
        }
        
        private double GetLowLastXBars(int x){
        
            List<double> lowPrices = new List<double>();
            
            for( int i =2; i <= x; i++){
                lowPrices.Add(Bars.Last(i).Low);
            }
            
            return lowPrices.Min();
        }
        
    }
}