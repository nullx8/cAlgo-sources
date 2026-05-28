using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Tradesize : Indicator
    {
        [Parameter("Stop Loss", DefaultValue = 0.0, Step = 0.0001)]
        public double StopLoss { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }
        string LogText = "";

        protected override void Initialize()
        {
            // Initialize and create nested indicators
        }

        public override void Calculate(int index)
        {
            var vwapLabel = Chart.DrawHorizontalLine("SL Price", StopLoss, Color.Yellow, 2);
            vwapLabel.IsInteractive = true;

            ChartObjects.DrawText("TradeSize-text", LogText, StaticPosition.TopLeft, Colors.Lime);
        }

        private double GetRiskSize(double pips, double risk = 1)
        {
            return Math.Min(Symbol.NormalizeVolumeInUnits((Account.Balance * risk / 100) / (pips * Symbol.PipValue), RoundingMode.Down), Symbol.VolumeInUnitsMax);
        }

    }
}
