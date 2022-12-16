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
    public class Half_Martingale_Hedging_Bot : Robot
    {
        private const string label = "Hedging Bot";
        private RelativeStrengthIndex rsi;
         
        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }
        
        [Parameter(DefaultValue = 20, MinValue = 5, MaxValue = 100, Step = 5)]
        public int HedgingPips { get; set; }
        
        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 0.5)]
        public double RewardRiskRatio { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }
        
        PendingOrder hedgingShortOrder;
        PendingOrder hedgingLongOrder;

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source,14);
            Positions.Closed += OnPositionClosed;
            PendingOrders.Filled += OnPendingOrderFilled;
        }
        
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var closedPosition = args.Position;
            
            //var positions = Positions.FindAll(label,SymbolName);
            
            if(closedPosition.TradeType == TradeType.Buy){
                hedgingLongOrder.Cancel();
            }else if(closedPosition.TradeType == TradeType.Sell){
                hedgingShortOrder.Cancel();
            }
        }

        private void OnPendingOrderFilled(PendingOrderFilledEventArgs args) { 
            //Hedging filled
            //Modify entry position, cancel tp.
            

        }

        protected override void OnTick()
        {

        }
        
        protected override void OnBar(){
            // Handle price updates here
            var longPositions = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPositions = Positions.Find(label, SymbolName, TradeType.Sell);
            
            if(LongSingal() && longPositions == null){
               var volumeInUnits = GetOptimalBuyUnit(HedgingPips,StopLossPrc);
               var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, null, HedgingPips*RewardRiskRatio);
               if(result.IsSuccessful){
                    double targePrice = result.Position.EntryPrice - (HedgingPips*Symbol.PipSize); 
                    var hedgeResult = PlaceStopOrder(TradeType.Sell, SymbolName, volumeInUnits,targePrice,label);
                    if(hedgeResult.IsSuccessful){
                       hedgingLongOrder = hedgeResult.PendingOrder;
                    }
                    
               }
                            
            }else if(ShortSignal() && shortPositions == null){
               var volumeInUnits = GetOptimalBuyUnit(HedgingPips,StopLossPrc);
               var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, null, HedgingPips*RewardRiskRatio);
                if(result.IsSuccessful){
                    double targePrice = result.Position.EntryPrice + (HedgingPips*Symbol.PipSize); 
                    var hedgeResult = PlaceStopOrder(TradeType.Buy, SymbolName, volumeInUnits,targePrice,label);
                    if(hedgeResult.IsSuccessful){
                        hedgingShortOrder = hedgeResult.PendingOrder;
                    }
               }
            }
        }

        protected override void OnStop()
        {

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
        
        private bool LongSingal(){

            return rsi.Result.Last(1) > 30 && rsi.Result.Last(2) < 30; //Sample signal
        }
        
        private bool ShortSignal(){

            return rsi.Result.Last(1) < 70 && rsi.Result.Last(2) > 70; //Sample signal
        }

        private double FakeUpperResistance(double entryPrice) {
            return entryPrice + (Symbol.PipSize * 80); 
        }

        private double FakeLowerResistance(double entryPrice)
        {
            return entryPrice + (Symbol.PipSize * 80);
        }
    }
    
    
}