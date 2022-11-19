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

        protected List<string> symbolList = new List<string>() { "EURUSD", "GBPUSD", "USDCAD" };

        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            telegram = new Telegram();
            symbolDisplay = SymbolName + TimeFrame;

            telegram.SendTelegram(ChatID, BotToken, symbolDisplay + " Bot Start");

            rsi = Indicators.RelativeStrengthIndex(Source, RsiPeriod);

            foreach (string symbol in symbolList)
            {
                Print(symbol);
            }

        }

        protected override void OnTick()
        {



        }

        protected override void OnBar()
        {

            if (rsi.Result.Last(1) <= RsiLowThres)
            { //Over Sold.
                telegram.SendTelegram(ChatID, BotToken, symbolDisplay + " is oversold on last bar");
            }
            else if (rsi.Result.Last(1) >= RsiHighThres)
            { //Over bought.
                telegram.SendTelegram(ChatID, BotToken, symbolDisplay + " is overbought on last bar");
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
        String pairName;
        String timeFrame;
        RelativeStrengthIndex rsi;
    }
}