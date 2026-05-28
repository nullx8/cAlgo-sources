// -------------------------------------------------------------------------------------------------
//
// ..
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FracalOrders : Robot
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Periods", DefaultValue = 14)]
        public int Periods { get; set; }

        [Parameter("SL", DefaultValue = 5)]
        public int SL { get; set; }

        [Parameter("TP", DefaultValue = 10)]
        public int TP { get; set; }

        [Parameter("Quantity (Lots)", DefaultValue = 1, MinValue = 0.01, Step = 0.01)]
        public double Quantity { get; set; }

        private RelativeStrengthIndex rsi;

        private Fractals fr;

        protected override void OnStart()
        {
            rsi = Indicators.RelativeStrengthIndex(Source, Periods);
            fr = Indicators.GetIndicator<Fractals>(5);
        }

        protected override void OnBar()
        {
            if (rsi.Result.LastValue > 70)
            {
                //Close(TradeType.Sell);
                Open(TradeType.Sell);
            }
            else if (rsi.Result.LastValue > 80)
            {
                Close(TradeType.Sell);
                //    Open(TradeType.Sell);
            }
            else if (rsi.Result.LastValue < 40)
            {
                Close(TradeType.Buy);
                // Open(TradeType.Sell);
            }
            else if (rsi.Result.LastValue < 30)
            {
                //Close(TradeType.Sell);
                Open(TradeType.Buy);
            }
        }

        private void Close(TradeType tradeType)
        {
            foreach (var position in Positions.FindAll("SampleRSI", Symbol, tradeType))
                ClosePosition(position);fract
        }

        private void Open(TradeType tradeType)
        {
            int i = 0;
            foreach (var positionb in Positions.FindAll("SampleRSI", Symbol, tradeType))
                i++;

            var position = Positions.Find("SampleRSI", Symbol, tradeType);
            var volumeInUnits = Symbol.QuantityToVolume(Quantity);

            if ((i < 2) || (Account.Equity > Account.Balance))
                ExecuteMarketOrder(tradeType, Symbol, volumeInUnits, "SampleRSI", SL, TP);
        }
    }
}
