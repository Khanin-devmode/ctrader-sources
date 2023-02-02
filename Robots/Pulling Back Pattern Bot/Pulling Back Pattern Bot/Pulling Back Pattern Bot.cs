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
    public class PullingBackPatternBot : Robot
    {

        public string Label = "Pulling Back Bot";
        
       [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 2, Step = 0.1)]
        public double RiskRewardRatio { get; set; }
        
       [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 200, Step = 5)]
        public double MinPullBackSize { get; set; }

        protected override void OnStart()
        {

        }

        protected override void OnTick()
        {
            // Handle price updates here
        }
        
        protected override void OnBar(){
        
            var shortPosition = Positions.Find(Label,SymbolName,TradeType.Sell);
            var longPosition = Positions.Find(Label,SymbolName,TradeType.Buy);
        
            if(IsPullingLongBack() && shortPosition == null){
            
            
                int slPips = Convert.ToInt16((Bars.Last(1).Close - Bars.Last(3).Open)/(2*Symbol.PipSize));

                int tpPips = Convert.ToInt16(slPips * RiskRewardRatio);
                
                double optimalBuyUnit = GetOptimalBuyUnit(slPips,0.02);
                var result = ExecuteMarketOrder(TradeType.Sell,SymbolName,optimalBuyUnit,Label,slPips,tpPips);
            
            }else if(IsPullingShortBack() && longPosition ==null){
                
                int slPips = Convert.ToInt16(Math.Abs((Bars.Last(1).Close - Bars.Last(3).Open))/(2*Symbol.PipSize));
                int tpPips = Convert.ToInt16(slPips * RiskRewardRatio);
            
                double optimalBuyUnit = GetOptimalBuyUnit(slPips,0.02);
                var result = ExecuteMarketOrder(TradeType.Buy,SymbolName,optimalBuyUnit,Label,slPips,tpPips);
            }
            
        
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        
        private bool IsPullingLongBack(){// n = last candle count
        
            //for(int i = 1 ; i <=n ; i++){
            //}
            
            bool isCloseHigher = Bars.Last(1).Close > Bars.Last(2).Close &&  Bars.Last(2).Close > Bars.Last(3).Close;
            bool isOpenHigher = Bars.Last(1).Open > Bars.Last(2).Open &&  Bars.Last(2).Open > Bars.Last(3).Open;
            
            double bar1Size = Math.Abs(Bars.Last(1).Close - Bars.Last(1).Open);    
            double bar2Size = Math.Abs(Bars.Last(2).Close - Bars.Last(2).Open); 
            double bar3Size = Math.Abs(Bars.Last(3).Close - Bars.Last(3).Open); 
            
            bool isGettingShorter = bar1Size < bar2Size && bar2Size < bar3Size;
            
            bool isBigEnough = Math.Abs((Bars.Last(1).Close - Bars.Last(3).Open))/Symbol.PipSize >= MinPullBackSize;
        
            return isCloseHigher && isOpenHigher && isGettingShorter && isBigEnough;
        }
        
        private bool IsPullingShortBack(){// n = last candle count
        
            //for(int i = 1 ; i <=n ; i++){
            //}
            
            bool isCloseLower = Bars.Last(1).Close < Bars.Last(2).Close &&  Bars.Last(2).Close < Bars.Last(3).Close;
            bool isOpenLower = Bars.Last(1).Open < Bars.Last(2).Open &&  Bars.Last(2).Open < Bars.Last(3).Open;
            
            double bar1Size = Math.Abs(Bars.Last(1).Close - Bars.Last(1).Open);    
            double bar2Size = Math.Abs(Bars.Last(2).Close - Bars.Last(2).Open); 
            double bar3Size = Math.Abs(Bars.Last(3).Close - Bars.Last(3).Open); 
            
            bool isGettingShorter = bar1Size < bar2Size && bar2Size < bar3Size;
            
            bool isBigEnough = Math.Abs((Bars.Last(1).Close - Bars.Last(3).Open))/Symbol.PipSize >= MinPullBackSize;
        
            return isCloseLower && isOpenLower && isGettingShorter && isBigEnough;
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