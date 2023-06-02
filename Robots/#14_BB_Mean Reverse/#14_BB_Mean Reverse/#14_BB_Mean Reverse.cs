using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

// Long: When close is lower than lower bb.
// Short: vice versa.
// TP: at middle band
// SL: ratio to TP
// Filter noise with ATR

//Result Comment: Condition is conflicting result in no trades met.

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class BB_Mean_Reverse : Robot
    {
        private string label;
        private BollingerBands bb;
        private AverageTrueRange atr;



        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 14, MinValue = 2, MaxValue = 60, Step = 2)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 0.5)]
        public double RewardRiskRatio { get; set; }

        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType { get; set; }

        [Parameter(DefaultValue = 0.002, MinValue = 0.001, MaxValue = 0.01, Step = 0.001)] //default value 20 pips, average range, will affect by timeframe.
        public double ATRValueThres { get; set; }


        //Telegram Parameter
        Telegram telegram;

        [Parameter(DefaultValue = false)]
        public bool NotifyOnOrder { get; set; }

        [Parameter("Bot Token", DefaultValue = "5680517295:AAGy2NnvXz72ZZWQIWuksP4nn3vYfo1f1EU", Group = "Telegram Notificatinons")]
        public string BotToken { get; set; }

        [Parameter("Chat ID", DefaultValue = "5068539927", Group = "Telegram Notificatinons")]
        public string ChatID { get; set; }

        protected override void OnStart()
        {

            label = "#14 BB Mean Reverse Bot: " + Symbol.Name;
            bb = Indicators.BollingerBands(Source, Period, 2, MAType);
            atr = Indicators.AverageTrueRange(Period, MAType);

            //Telegram initialize.
            if (NotifyOnOrder)
            {
                telegram = new Telegram();

                telegram.SendTelegram(ChatID, BotToken, $"{label} Start");
            }

        }


        protected override void OnBar()
        {

            if (atr.Result.LastValue > ATRValueThres)
            {

                var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
                var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);

                if (LongSignal() && longPosition == null)
                {
                
                    int tpPips = Convert.ToInt16((bb.Main.LastValue - bb.Bottom.LastValue)/Symbol.PipSize);
                    int slPips = Convert.ToInt16(tpPips/RewardRiskRatio);
                    
                    var volumeInUnits = GetOptimalBuyUnit(slPips, StopLossPrc);
                
                    var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, slPips, tpPips);
                    
                    if (NotifyOnOrder)
                    {
                        NotifyTelegram(result);
                    }
                   

                }

                if (ShortSignal() && shortPosition == null)
                {
                    int tpPips = Convert.ToInt16((bb.Top.LastValue - bb.Main.LastValue)/Symbol.PipSize);
                    int slPips = Convert.ToInt16(tpPips/RewardRiskRatio);
                    
                    var volumeInUnits = GetOptimalBuyUnit(slPips, StopLossPrc);
                    
                    var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, slPips, tpPips);
                    
                    if (NotifyOnOrder)
                    {
                        NotifyTelegram(result);
                    }
                }




            }


        }

        protected override void OnStop()
        {
            if (NotifyOnOrder)
            {
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
            stopLossAsset = accEquity * stopLossPrc;
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well       
            slChartSize = stopLossPips * Symbol.PipSize;
            optimalLotSizeInUnit = stopLossQuote / slChartSize;

            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);

        }

        protected bool LongSignal()
        {
            return Bars.Last(1).Close < bb.Bottom.LastValue;
        }

        protected bool ShortSignal()
        {
            return Bars.Last(1).Close > bb.Top.LastValue;
        }
        
        private void NotifyTelegram(TradeResult result){
            var position = result.Position;
            if (result.IsSuccessful){
                telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] {position.TradeType} {position.SymbolName} at {position.EntryPrice}");
            }else{
                telegram.SendTelegram(ChatID, BotToken, $"Error executing market order {result.Error}");
            }
        }

    }


}