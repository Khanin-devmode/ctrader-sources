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
    public class PositionSizingFunction : Robot
    {
    
        [Parameter(DefaultValue = 30)]
        public int StopLossPips { get; set; }
        
        [Parameter(DefaultValue = 0.02)]
        public double StopLossPrc { get; set; }

        protected override void OnStart()
        {
            
            Print(GetOptimalBuyUnit(StopLossPips,StopLossPrc));

        }

        protected override void OnTick()
        {
            // Handle price updates here
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }
        
        protected double GetOptimalBuyUnit(int stopLossPips, double stopLossPrc)
        {
        
            double accEquity;
            string assetCurrency;
            string baseCurrency;
            string quoteCurency;
            double stopLossAsset;
            double stopLossQuote;
            double slChartSize;
            double optimalLotSize;
            double optimalLotSizeInUnit;
                
            accEquity = Account.Equity;
            Print("Account Equity: " + accEquity); 
            
            assetCurrency = Account.Asset.Name;
            baseCurrency = SymbolName.Substring(0,3);
            quoteCurency = SymbolName.Substring(SymbolName.Length - 3);
            
            stopLossAsset = accEquity*stopLossPrc;
            Print("Stop Loss in " + assetCurrency + ": "  + stopLossAsset + " " + assetCurrency);
            
            stopLossQuote = stopLossAsset * (Symbol.TickSize / Symbol.TickValue); //Checked, work pretty well
            Print("Stop Loss in Quote: " + stopLossQuote + " " + quoteCurency); 
            
            //pip on chart = pips * pip size
            slChartSize = stopLossPips * Symbol.PipSize;
            
            Print("Stop Loss Chart pip: " + slChartSize);
            
            Print("Tick Value: " + Symbol.TickValue);
            Print("Tick Size: " + Symbol.TickSize);
            Print("Tick Size/Value: " + (Symbol.TickSize/Symbol.TickValue));
            Print("Pip Value: " + Symbol.PipValue);
            Print("Pip Size: " + Symbol.PipSize);
            Print("Pip Size/Value: " + (Symbol.PipSize/Symbol.PipValue));
            Print("Lot Size: " + Symbol.LotSize);
            Print("Digits: " + Symbol.Digits);

            optimalLotSizeInUnit = stopLossQuote/slChartSize;
            Print("Optimal lot size in unit: " + optimalLotSizeInUnit + " " + baseCurrency);
            
            optimalLotSize = stopLossQuote/ slChartSize / Symbol.LotSize;
            Print("Optimal Lot size: " + optimalLotSize);
            
            Print("^^^^^^^^^^^^^^^^^^^^OK^^^^^^^^^^^^^^^^^^^^^");
            
            return Symbol.NormalizeVolumeInUnits(optimalLotSizeInUnit, RoundingMode.Up);
            
        }
        
    }
}