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
    public class SupportResistanceBot : Robot
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }

        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            Print(Message);
            
            var length = Chart.MaxVisibleBars;
            
            for (var i = 1 ; i< length; i++){
            
               if( i > 2){
                    if(IsResistance(i)){
                       Print($"Bars {i} is resistance");
                    }
                }

            }
            
            
            
        }

        protected override void OnTick()
        {
            // Handle price updates here
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        protected bool IsResistance(int i){
            
            Print(i);
        
            bool cond1 = Bars[i].High > Bars[i-1].High;
            bool cond2 = Bars[i-1].High > Bars[i-2].High;
            
            bool cond3 = Bars[i].High > Bars[i+1].High;
            bool cond4 = Bars[i+1].High > Bars[i+2].High;
            
            return cond1 && cond2 && cond3 && cond4;
        
        }
    }
}