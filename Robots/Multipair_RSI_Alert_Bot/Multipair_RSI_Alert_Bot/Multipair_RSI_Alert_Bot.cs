using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.FullAccess)]
    public class Multipair_RSI_Alert_bot : Robot
    {

        protected DataSeries Source;
        protected RelativeStrengthIndex rsi;

        Telegram telegram;

        protected String symbolDisplay;

        [Parameter("Bot Token", DefaultValue = "5680517295:AAGy2NnvXz72ZZWQIWuksP4nn3vYfo1f1EU", Group = "Telegram Notificatinons")]
        public string BotToken { get; set; }

        [Parameter("Chat ID", DefaultValue = "5068539927", Group = "Telegram Notificatinons")]
        public string ChatID { get; set; }

        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2, Group = "RSI")]
        public int RsiPeriod { get; set; }

        [Parameter(DefaultValue = 65, Group = "RSI")]
        public int RsiHighThres { get; set; }

        [Parameter(DefaultValue = 35, Group = "RSI")]
        public int RsiLowThres { get; set; }

        private List<string> symbolList = new List<string>() { "EURUSD", "GBPUSD", "USDCAD" };
        private List<PairInfo> pairInfoList = new List<PairInfo>();

        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            telegram = new Telegram();

            telegram.SendTelegram(ChatID, BotToken," Bot Start");

            rsi = Indicators.RelativeStrengthIndex(Source, RsiPeriod);

            foreach (string symbol in symbolList)
            {

                var pairInfo = new PairInfo();
                pairInfo.pairName = symbol;
                pairInfo.bars = MarketData.GetBars(TimeFrame, symbol);
                pairInfo.rsi = Indicators.RelativeStrengthIndex(pairInfo.bars.ClosePrices, RsiPeriod);

                pairInfoList.Add(pairInfo);
            }

        }

        protected override void OnTick()
        {



        }

        protected override void OnBar()
        {
            foreach (PairInfo pair in pairInfoList) {

                var rsiResult = pair.rsi.Result;

                if (rsiResult.Last(1) <= RsiLowThres)
                { //Over Sold.
                    telegram.SendTelegram(ChatID, BotToken, pair.pairName + " " + TimeFrame + " is oversold on last bar" + "rsi: "+rsiResult.Last(1));
                }
                else if (rsiResult.Last(1) >= RsiHighThres)
                { //Over bought.
                    telegram.SendTelegram(ChatID, BotToken, pair.pairName + " " +TimeFrame + " is overbought on last bar" + "rsi: " + rsiResult.Last(1));
                }


            }
                
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
            telegram.SendTelegram(ChatID, BotToken, symbolDisplay + " Bot Stop.");
        }
    }

    public class PairInfo
    {   
        public String pairName;
        public String timeFrame;
        public RelativeStrengthIndex rsi;
        public Bars bars;

    }
}