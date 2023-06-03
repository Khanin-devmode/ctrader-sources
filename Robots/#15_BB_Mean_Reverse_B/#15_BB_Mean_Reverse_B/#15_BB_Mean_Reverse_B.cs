using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

// --ENTRY--
// Filter noise with ATR
// Long: When close is lower than lower bb and low is higher than long MA.
// Short: vice versa.
// --Exit--
// Long: When close is higher than main bb.
// Short: When close is lower than main bb.
// SL: Size of the band e.g. BB Top - Bottom with some ratio.




namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class BB_Mean_Reverse : Robot
    {
        private string label;
        private BollingerBands bb;
        private AverageTrueRange atr;
        private MovingAverage longMa;

        [Parameter("Source", DefaultValue = "Close")]
        public DataSeries Source { get; set; }

        [Parameter(DefaultValue = 14, MinValue = 2, MaxValue = 60, Step = 2)]
        public int ShortPeriod { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 20, MaxValue = 90, Step = 10)]
        public int LongPeriod { get; set; }

        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }
        
        [Parameter(DefaultValue = 20, MinValue = 10, MaxValue = 60, Step = 5)]
        public int SlPips { get; set; }

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

            label = "#15 BB Mean Reverse B Bot: " + Symbol.Name;
            bb = Indicators.BollingerBands(Source, ShortPeriod, 2, MAType);
            atr = Indicators.AverageTrueRange(ShortPeriod, MAType);
            longMa = Indicators.MovingAverage(Source,LongPeriod, MAType);

            //Telegram initialize.
            if (NotifyOnOrder)
            {
                telegram = new Telegram();

                telegram.SendTelegram(ChatID, BotToken, $"{label} Start");
            }

        }


        protected override void OnBar()
        {   
        
            var longPosition = Positions.Find(label, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(label, SymbolName, TradeType.Sell);
            
            //ENTRY
            if (atr.Result.LastValue > ATRValueThres)
            {

                if (LongSignal() && longPosition == null)
                {

                    var volumeInUnits = GetOptimalBuyUnit(SlPips, StopLossPrc);

                    var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, label, SlPips, null);

                    if (NotifyOnOrder)
                    {
                        NotifyTelegram(result);
                    }

                }

                if (ShortSignal() && shortPosition == null)
                {

                    var volumeInUnits = GetOptimalBuyUnit(SlPips, StopLossPrc);

                    var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, label, SlPips, null);

                    if (NotifyOnOrder)
                    {
                        NotifyTelegram(result);
                    }
                }

            }
            
            //EXIT
            if (longPosition != null && LongExitSignal())
            {
                ClosePosition(longPosition);
            }
            if (shortPosition != null && ShortExitSignal())
            {
                ClosePosition(shortPosition);
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
            return Bars.Last(1).Close < bb.Bottom.Last(1) && Bars.Last(1).Low > longMa.Result.Last(1);
        }

        protected bool ShortSignal()
        {
            return Bars.Last(1).Close > bb.Top.Last(1) && Bars.Last(1).High < longMa.Result.Last(1);
        }

        protected bool LongExitSignal()
        {
            return Bars.Last(1).Close > bb.Main.Last(1);
        }

        protected bool ShortExitSignal()
        {
            return Bars.Last(1).Close < bb.Main.Last(1);
        }

        private void NotifyTelegram(TradeResult result)
        {
            var position = result.Position;
            if (result.IsSuccessful)
            {
                telegram.SendTelegram(ChatID, BotToken, $"[{position.Id}] {position.TradeType} {position.SymbolName} at {position.EntryPrice}");
            }
            else
            {
                telegram.SendTelegram(ChatID, BotToken, $"Error executing market order {result.Error}");
            }
        }

    }


}