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
    public class RSI_Mean_Reverse : Robot
    {
        private RelativeStrengthIndex rsi;
        protected DataSeries Source;
        
        [Parameter(DefaultValue = 70, MinValue = 65, MaxValue = 75, Step = 5)]
        public int RsiHigh { get; set; }
        
        [Parameter(DefaultValue = 30, MinValue = 25, MaxValue = 35, Step = 5)]
        public int RsiLow { get; set; }
        
        [Parameter(DefaultValue = 14, MinValue = 4, MaxValue = 30, Step = 2)]
        public int RsiPeriod { get; set; }
        

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source,RsiPeriod);
        }

        protected override void OnTick()
        {
            // Handle price updates here
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
    }
}