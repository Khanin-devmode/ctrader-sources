using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;


// Behavior
// long: 
// when lastbar close is lower than bb low &
// oversold can be very long.
// 
//
//
//
//
//
//
//

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None)]
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
        
        private const string label = "BB Breakout version A bot";
        
        protected DataSeries Source;

        protected override void OnStart()
        {
            
            bb = Indicators.BollingerBands(Source,Period,2,MovingAverageType.Simple);
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

            
            if (BuySignal() && longPosition == null)
            {
                    
                    int slPips = Convert.ToInt32((Symbol.Ask - Bars.LastBar.Low)/0.0001);
                    int tpPips = Convert.ToInt32(slPips * TpRatio);
                    

                    
                    if(slPips > 10){
                    
                    
                    Print("Long");
                    Print(Bars.LastBar.Low);
                    Print(Symbol.Ask);
                    Print(Bars.LastBar.High - Symbol.Ask);
                    Print(Symbol.PipSize);
                    
                    Print(slPips);
                        var volumeInUnits = GetOptimalBuyUnit(slPips,StopLossPrc);
                        ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, slPips,tpPips,"",true);
                    }
                    

                
            }else if(ShortSignal() && shortPosition == null){
                    
                    int slPips = Convert.ToInt32((Symbol.Bid - Bars.LastBar.High)/0.0001);
                    int tpPips = Convert.ToInt32(slPips * TpRatio);
                    

                    if(slPips >10){
                    
                    
                                        Print("Short");
                    Print(Bars.LastBar.High);
                    Print(Symbol.Bid);
                    Print(Bars.LastBar.High - Symbol.Bid);
                    Print(Symbol.PipSize);
                    Print(slPips);
                    
                        var volumeInUnits = GetOptimalBuyUnit(slPips,StopLossPrc);
                        ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, slPips, tpPips, "", true);                    
                    }

            } 
        
        }

        
        private bool BuySignal(){
        
            var top = bb.Top.Last(1);
        
            bool isBreakingBB = Bars.LastBar.Close > top;
            bool isStrongTrend = dms.ADX.LastValue > 25;
        
            return isBreakingBB && IsBreakingResistance() && isStrongTrend;        
        }
        
        private bool ShortSignal(){
        
            var bottom = bb.Bottom.Last(1);
        
            bool isBreakingBB = Bars.LastBar.Close < bottom;
            bool isStrongTrend = dms.ADX.LastValue > 25;
        
            return isBreakingBB && IsBreakingSupport() && isStrongTrend;        
        }
        
        private bool IsBreakingResistance()
        {
            for(var i = 2 ; i <= BackwardBars; i++ ){
                if(!(Bars.Last(1).High > Bars.Last(i).High)){
                       
                    return false;
                }
               
            }
            
             return true;
        }
        
        private bool IsBreakingSupport()
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
            stopLossAsset = accEquity*stopLossPrc;
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well
            slChartSize = stopLossPips * Symbol.PipSize;
            optimalLotSizeInUnit = stopLossQuote/slChartSize;
  
            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);
            
        }
        
    }
    
    
}