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

        Bars dBars;
        int n;
        protected override void OnStart()
        {
            // To learn more about cTrader Automate visit our Help Center:
            // https://help.ctrader.com/ctrader-automate

            dBars = MarketData.GetBars(TimeFrame.Daily);
            n = dBars.Count;
            
            Print(n);

            for (int i = 100; i >= 3; i--)
            {
                Print(dBars.Last(i).OpenTime);
                if (i < (n - 2))
                {
                    //if (IsResistance(i))
                    //{
                    //    Print($"Bars {i} is resistance");
                    //    Print();
                    //    Chart.DrawHorizontalLine($"Resistance Line {i}", Bars[i].High, Color.Red);
                    //    Print(Bars[i].OpenTime);


                    //}
                    var cond1 = dBars.Last(i).High > dBars.Last(i - 1).High;
                    var cond2 = dBars.Last(i-1).High > dBars.Last(i - 2).High;

                    var cond3 = dBars.Last(i).High > dBars.Last(i + 1).High;
                    var cond4 = dBars.Last(i + 1).High > dBars.Last(i + 2).High;

                    if (cond1 && cond2 && cond3 && cond4) {
                        Chart.DrawHorizontalLine($"Resistance Line {i}", dBars.Last(i).High, Color.Red);
                        
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

        protected bool IsResistance(int i)
        {
            bool cond1 = Bars[i].High > Bars[i - 1].High;
            bool cond2 = Bars[i - 1].High > Bars[i - 2].High;

            bool cond3 = Bars[i].High > Bars[i + 1].High;
            bool cond4 = Bars[i + 1].High > Bars[i + 2].High;

            return cond1 && cond2 && cond3 && cond4;

        }
    }
}