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
    public class SimpleSMACrossOver : Robot
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }
        
        [Parameter(DefaultValue = 20)]
        public int FastMAPeriod { get; set; }
        
        [Parameter(DefaultValue = 50)]
        public int SlowMAPeriod { get; set; }
        
        [Parameter(DefaultValue = 0)]
        public int CrossPeriod { get; set; }
        
        [Parameter("Source")]
        public DataSeries Source { get; set; }
        
        [Parameter(DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType MAType { get; set; }
        
        private MovingAverage fastMA;
        private MovingAverage slowMA;
        
        protected string Label = "SMA_CROSS_BOT";
        protected double stopLoss = 0.02;
        
        private double accEquity;
        private double maxStopLossAmount;
        //private double stopLossPips;
        private double maxLossInQuoteCurrency;
       
        
        public Position[] BotPositions
        {
            get
            {
                return Positions.FindAll(Label);
            }
        }

        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate
            
            fastMA = Indicators.MovingAverage(Source,FastMAPeriod,MAType);
            slowMA = Indicators.MovingAverage(Source,SlowMAPeriod,MAType);
            
            
            
            //maxLossInQuoteCurrency = maxStopLossAmount / Symbol.TickSize;
            accEquity = Account.Equity;
            maxStopLossAmount = accEquity * stopLoss;
            Print(Symbol.Ask);
            Print("StopLoss in USD: " + maxStopLossAmount);
      
            var fullTickValue = Symbol.TickValue/Symbol.TickSize;
            Print("Full Tick Value: " + fullTickValue);
            
            
            maxLossInQuoteCurrency = maxStopLossAmount / fullTickValue;
            
            Print("Stop Loss in Quote price: " + maxLossInQuoteCurrency);
            
            Print("Account Equity: " + accEquity);
            Print("Symbol tick value " + Symbol.TickValue);
            Print("Symbol tick size " + Symbol.TickSize);
            Print("Symbol pip size: " + Symbol.PipSize);
            Print("Symbol pip value: " + Symbol.PipValue);
            
            var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, 2000);
            if(result.IsSuccessful){
                Print(Account.Balance);
            }
            
        }

        protected override void OnTick()
        {
            // Handle price updates here
            
        }
        
        protected override void OnBar()
        {
        


            if (fastMA.Result.HasCrossedAbove(slowMA.Result,CrossPeriod))
            {
                
               
                //Close Short
                //ClosePositions(TradeType.Sell);
                
                //Buy Long
                //ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
                
                
            }
            else if (fastMA.Result.HasCrossedBelow(slowMA.Result, CrossPeriod))
            {
                //ClosePositions(TradeType.Buy);

               // ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);

            }
            
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        private void ClosePositions(TradeType tradeType)
        {
            foreach (var position in BotPositions)
            {
                if (position.TradeType != tradeType) continue;

                ClosePosition(position);
            }
        }
    }
}