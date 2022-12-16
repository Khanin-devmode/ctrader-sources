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
        enum TradePhase
        {
            AimTP,
            Hedging,
            AimBE,
        }
        
        
        TradePhase currentPhase = TradePhase.AimTP;
    
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
        double uppperResistanceLine;
        double lowerResistanceLine;
        double standardVolume;

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source,14);
            Positions.Closed += OnPositionClosed;
            PendingOrders.Filled += OnPendingOrderFilled;
        }
        
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            Position closedPosition = args.Position;
            
            //If take profit cancel all open hedge order;
            if(closedPosition.TradeType == TradeType.Buy){
                hedgingLongOrder.Cancel();
            }else if(closedPosition.TradeType == TradeType.Sell){
                hedgingShortOrder.Cancel();
            }
        }

        private void OnPendingOrderFilled(PendingOrderFilledEventArgs args) { 
            //Hedging filled
            //Modify entry position, cancel tp.
            Position[] allPositions = Positions.FindAll(label,SymbolName);
            foreach (Position position in allPositions){
                position.ModifyTakeProfitPips(null);
            }
            
            
            currentPhase = TradePhase.Hedging;
            //Set resistance for price target.
            uppperResistanceLine = FakeUpperResistance(args.Position.EntryPrice);
            lowerResistanceLine = FakeLowerResistance(args.Position.EntryPrice);
            
        }

        protected override void OnTick()
        {
            if(currentPhase == TradePhase.Hedging){
                
                if(Symbol.Bid >= (uppperResistanceLine - (HedgingPips*Symbol.PipSize)/2)){
                    //Exit Profit position before reaching resistance line by hedgingpips/2
                    Position longPosition = Positions.Find(label,SymbolName); //There should be only one hedging position
                    var closeResult = ClosePosition(longPosition);
                    if(closeResult.IsSuccessful){
                        
                        //enter short
                        var shortResult = ExecuteMarketOrder(TradeType.Sell,SymbolName,standardVolume,label);
                        if(shortResult.IsSuccessful){
                            //create hedge order
                            var hedgeVolume = GetTotalTradeVolume(TradeType.Sell);
                            double entryPrice = shortResult.Position.EntryPrice + (HedgingPips*Symbol.PipSize);
                            var stopOrderResult = PlaceStopOrder(TradeType.Buy,SymbolName,hedgeVolume,entryPrice);
                            if(stopOrderResult.IsSuccessful){
                                //change to aim BE phase.
                                currentPhase = TradePhase.AimBE;
                            }
                            
                        }
                    }
                    
                }else if(Symbol.Ask <= (lowerResistanceLine- (HedgingPips*Symbol.PipSize)/2)){
                    //Exit Profit position before reaching resistance line by hedgingpips/2
                    Position shortPosition = Positions.Find(label,SymbolName); //There should be only one hedging position
                    var closeResult = ClosePosition(shortPosition);
                    if(closeResult.IsSuccessful){
                        //enter long position
                        var longResult = ExecuteMarketOrder(TradeType.Buy,SymbolName,standardVolume);
                        if(longResult.IsSuccessful){
                            //create hedge order
                            double hedgeVolume = GetTotalTradeVolume(TradeType.Buy);
                            double entryPrice = longResult.Position.EntryPrice - (HedgingPips*Symbol.PipSize);
                            var stopOrderResult = PlaceStopOrder(TradeType.Sell,SymbolName,hedgeVolume,entryPrice);
                            if(stopOrderResult.IsSuccessful){
                                currentPhase = TradePhase.AimBE;
                            }
                        }
                    }    
                }          
            
                
            
            }
            
            if(currentPhase == TradePhase.AimBE){
                
                double currentNetProfit = 0; 
                Position[] allPosition = Positions.FindAll(label,SymbolName);
                foreach (Position position in allPosition){
                    currentNetProfit = currentNetProfit + position.GrossProfit;
                }
                
                if(currentNetProfit >= 0){
                    foreach (Position position in allPosition){
                        ClosePositionAsync(position);
                    }    
                    
                    currentPhase = TradePhase.AimTP;
                                     
                }
            
            }
            
            
        }
        
        protected override void OnBar(){
            // Handle price updates here
            var longPositions = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPositions = Positions.Find(label, SymbolName, TradeType.Sell);
            
            if(currentPhase == TradePhase.AimTP){
                if(LongSingal() && longPositions == null){
                   standardVolume = GetOptimalBuyUnit(HedgingPips,StopLossPrc);
                   var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, standardVolume, label, null, HedgingPips*RewardRiskRatio);
                   if(result.IsSuccessful){
                        //create hedgeorder
                        double targePrice = result.Position.EntryPrice - (HedgingPips*Symbol.PipSize); 
                        var hedgeResult = PlaceStopOrder(TradeType.Sell, SymbolName, standardVolume,targePrice,label);
                        if(hedgeResult.IsSuccessful){
                           hedgingLongOrder = hedgeResult.PendingOrder;

                        }
                        
                   }
                                
                }else if(ShortSignal() && shortPositions == null){
                   standardVolume = GetOptimalBuyUnit(HedgingPips,StopLossPrc);
                   var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, standardVolume, label, null, HedgingPips*RewardRiskRatio);
                    if(result.IsSuccessful){
                        //create hedgeorder
                        double targePrice = result.Position.EntryPrice + (HedgingPips*Symbol.PipSize); 
                        var hedgeResult = PlaceStopOrder(TradeType.Buy, SymbolName, standardVolume,targePrice,label);
                        if(hedgeResult.IsSuccessful){
                            hedgingShortOrder = hedgeResult.PendingOrder;
                        }
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
        
        private double GetTotalTradeVolume(TradeType tradeType){
            double totalVolume = 0;
            Position[] positions = Positions.FindAll(label,SymbolName,tradeType);
            foreach (Position position in positions){
                totalVolume = totalVolume + position.VolumeInUnits;
            }
            
            return totalVolume;
            
        }
    }
    
    
}