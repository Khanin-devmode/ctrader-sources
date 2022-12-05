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
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class RSI_Mean_Reverse : Robot
    {
        private const string label = "RSI Reverse Bot";
        private RelativeStrengthIndex rsi;

        Telegram telegram;
        
        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 70, MinValue = 65, MaxValue = 75, Step = 5)]
        public int RsiHigh { get; set; }
        
        [Parameter(DefaultValue = 30, MinValue = 25, MaxValue = 35, Step = 5)]
        public int RsiLow { get; set; }
        
        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2)]
        public int RsiPeriod { get; set; }
        
        [Parameter(DefaultValue = 40, MinValue = 30, MaxValue = 50, Step = 5)]
        public int ShortExitRsi { get; set; }
        
        [Parameter(DefaultValue = 60, MinValue = 50, MaxValue = 70, Step = 5)]
        public int LongExitRsi { get; set; }
        
        [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 150, Step = 5)]
        public int TakeProfitPips { get; set; }
        
        [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 150, Step = 5)]
        public int StopLossPips { get; set; }
        
        [Parameter(DefaultValue = 0.02, MinValue = 0.01, MaxValue = 0.05, Step = 0.01)]
        public double StopLossPrc { get; set; }

        [Parameter("Bot Token", DefaultValue = "5680517295:AAGy2NnvXz72ZZWQIWuksP4nn3vYfo1f1EU", Group = "Telegram Notificatinons")]
        public string BotToken { get; set; }

        [Parameter("Chat ID", DefaultValue = "5068539927", Group = "Telegram Notificatinons")]
        public string ChatID { get; set; }
        
        [Parameter(DefaultValue = false)]
        public bool NotifyOnOrder { get; set; }


        protected override void OnStart()
        {
            if(NotifyOnOrder) {
                telegram = new Telegram();

                telegram.SendTelegram(ChatID, BotToken, $"{label} Start");
            }


            rsi = Indicators.RelativeStrengthIndex(Source,RsiPeriod);
        }

        protected override void OnTick()
        {

            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);

            if(shortPosition != null && rsi.Result.LastValue <= ShortExitRsi){
                var result = ClosePosition(shortPosition);
                if(NotifyOnOrder){
                    var position = result.Position;
                    if(result.IsSuccessful){
                        telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] Closing SHORT poition {position.SymbolName}. Profit {position.NetProfit} USD");
                    }else{
                        telegram.SendTelegram(ChatID, BotToken, $"Error closing position {result.Error}");
                    }
                }
            }else if (longPosition != null && rsi.Result.LastValue >= LongExitRsi){
                var result = ClosePosition(longPosition);
                if(NotifyOnOrder){
                    var position = result.Position;
                    if(result.IsSuccessful){
                        telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] Closing LONG poition {position.SymbolName}. Profit {position.NetProfit} USD");
                    }else{
                        telegram.SendTelegram(ChatID, BotToken, $"Error closing position {result.Error}");
                    }
                }                
                
            }
        }
        
        protected override void OnBar(){
        
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);

            var volumeInUnits = GetOptimalBuyUnit(StopLossPips,StopLossPrc);

            //if(shortPosition != null && rsi.Result.Last(1) <= ExitRsi){
            //    ClosePosition(shortPosition);
            //}else if (longPosition != null && rsi.Result.Last(1) >= ExitRsi){
            //    ClosePosition(longPosition);
            //}
            
            
            if (rsi.Result.Last(1) > RsiLow && rsi.Result.Last(2) < RsiLow && longPosition == null){
                var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, StopLossPips, TakeProfitPips);
                
                if(NotifyOnOrder){
                    var position = result.Position;
                    if(result.IsSuccessful){
                        telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] Buying {position.SymbolName} at {position.EntryPrice}");
                    }else{
                        telegram.SendTelegram(ChatID, BotToken, $"Error executing market order {result.Error}");
                    }
                }

                
            } else if (rsi.Result.Last(1) < RsiHigh && rsi.Result.Last(2) > RsiHigh && shortPosition == null){
                var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, StopLossPips, TakeProfitPips);
                if(NotifyOnOrder){
                    var position = result.Position;
                    if(result.IsSuccessful){
                        telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] Selling {position.SymbolName} at {position.EntryPrice}");
                    }else{
                        telegram.SendTelegram(ChatID, BotToken, $"Error executing market order {result.Error}");
                    }
                }                
            }
        }

        protected override void OnStop()
        {
            if (NotifyOnOrder) {
                telegram.SendTelegram(ChatID, BotToken, $"{label} Stop");
            }


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