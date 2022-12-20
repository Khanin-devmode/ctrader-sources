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

        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 0.5)]
        public double RewardRiskRatio { get; set; }
        
        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 0.5)]
        public double HedgingRatio { get; set; }

        [Parameter(DefaultValue = 20, MinValue = 5, MaxValue = 100, Step = 5)]
        public int RecoveryZonePips { get; set; }
        
        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }
        
        double stdLotSize;
        double upperZonePrice;
        double lowerZonePrice;
        double totalLongUnit = 0;
        double totalShortUnit = 0;
        private Position[] allPosition;
        

        protected override void OnStart()
        {

        }

        protected override void OnTick()
        {
            
           //when crossing the zone
           //crossing lower zone, short to with higher lot size.
           if(Symbol.Ask <= lowerZonePrice && totalLongUnit > totalShortUnit ){
           
               double shortUnitInVolume = (totalLongUnit*HedgingRatio) - totalShortUnit;
               var shortResult = ExecuteMarketOrder(TradeType.Sell,SymbolName,shortUnitInVolume);
               
               
           }
           
        }
        
        protected override void OnBar(){
            
            if(LongSignal()){
                
                stdLotSize = GetOptimalBuyUnit(RecoveryZonePips,StopLossPrc);
                var result = ExecuteMarketOrder(TradeType.Buy,SymbolName,stdLotSize,label,null,RecoveryZonePips * RewardRiskRatio);
                if(result.IsSuccessful){
                    upperZonePrice = result.Position.EntryPrice;
                    lowerZonePrice = upperZonePrice - (RecoveryZonePips * Symbol.PipSize);
                    totalLongUnit = totalLongUnit + result.Position.VolumeInUnits;
                
                }
            
            }else if(LongSignal()){
                
                stdLotSize = GetOptimalBuyUnit(RecoveryZonePips,StopLossPrc);
                var result = ExecuteMarketOrder(TradeType.Sell,SymbolName,stdLotSize,label,null,RecoveryZonePips * RewardRiskRatio);
                if(result.IsSuccessful){
                    lowerZonePrice = result.Position.EntryPrice;
                    upperZonePrice = lowerZonePrice + (RecoveryZonePips * Symbol.PipSize);
                    totalShortUnit = totalShortUnit + result.Position.VolumeInUnits;
                
                }
            
            }
            
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        private bool LongSignal(){
            return true;
        }
        
        private bool ShortSignal(){
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