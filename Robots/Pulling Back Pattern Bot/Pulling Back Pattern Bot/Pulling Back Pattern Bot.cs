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
        [Parameter(DefaultValue = "Hello world!")]
        public string Label { get; set; }

        protected override void OnStart()
        {

        }

        protected override void OnTick()
        {
            // Handle price updates here
        }
        
        protected override void OnBar(){
        
            var shortPosition = Positions.Find(Label,SymbolName,TradeType.Sell);
        
        
            if(IsPullingLongBack(2) && shortPosition == null){
            
                int tpPips = Convert.ToInt16((Bars.Last(1).Close - Bars.Last(3).Open)/(2*Symbol.PipSize));
                Print(tpPips);
                int slPips = (tpPips/2);
                double optimalBuyUnit = GetOptimalBuyUnit(slPips,0.02);
                var result = ExecuteMarketOrder(TradeType.Sell,SymbolName,optimalBuyUnit,Label,slPips,tpPips);
            
            }
            
        
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        
        private bool IsPullingLongBack(int n){// n = last candle count
        
            //for(int i = 1 ; i <=n ; i++){
            //}
            
            bool isCloseHigher = Bars.Last(1).Close > Bars.Last(2).Close &&  Bars.Last(2).Close > Bars.Last(3).Close;
            bool isOpenHigher = Bars.Last(1).Open > Bars.Last(2).Open &&  Bars.Last(2).Open > Bars.Last(3).Open;
            
            double bar1Size = Bars.Last(1).Close - Bars.Last(1).Open;    
            double bar2Size = Bars.Last(2).Close - Bars.Last(2).Open; 
            double bar3Size = Bars.Last(3).Close - Bars.Last(3).Open; 
            
            bool isGettingShorter = bar1Size < bar2Size && bar2Size < bar3Size;
        
            return isCloseHigher && isOpenHigher && isGettingShorter;
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