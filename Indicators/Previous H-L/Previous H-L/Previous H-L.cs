using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class PreviousHL : Indicator
    {
        [Parameter("Length", DefaultValue = 10)]
        public int length { get; set; }

        [Parameter("LookBack", DefaultValue = 1)]
        public int lback { get; set; }

        [Parameter("Daily Colors", Group = "Colours", DefaultValue = "Yellow")]
        public string dc { get; set; }
        [Parameter("Monthly Colors", Group = "Colours", DefaultValue = "Red")]
        public string mc { get; set; }
        [Parameter("Weekly Colors", Group = "Colours", DefaultValue = "Green")]
        public string wc { get; set; }

        [Parameter("Show Daily HL", Group = "Prices", DefaultValue = true)]
        public bool showD { get; set; }
        [Parameter("Show Week HL", Group = "Prices", DefaultValue = true)]
        public bool showW { get; set; }
        [Parameter("Show Month HL", Group = "Prices", DefaultValue = true)]
        public bool showM { get; set; }
        [Parameter("Show Close Prices", Group = "Prices", DefaultValue = true)]
        public bool showClose { get; set; }

        [Parameter("Show Daily Labels", Group = "Labels", DefaultValue = true)]
        public bool showDLabs { get; set; }
        [Parameter("Show Weekly Labels", Group = "Labels", DefaultValue = true)]
        public bool showWLabs { get; set; }
        [Parameter("Show Monthly Labels", Group = "Labels", DefaultValue = true)]
        public bool showMLabs { get; set; }


        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private MarketSeries w, m, d;

        protected override void Initialize()
        {
            w = MarketData.GetSeries(TimeFrame.Weekly);
            d = MarketData.GetSeries(TimeFrame.Daily);
            m = MarketData.GetSeries(TimeFrame.Monthly);
        }

        public override void Calculate(int index)
        {
            if (showW)
            {
                Chart.DrawTrendLine("wh", index, w.High[w.High.Count - lback - 1], index - length, w.High[w.High.Count - lback - 1], Color.FromName(wc));
                Chart.DrawTrendLine("wl", index, w.Low[w.High.Count - lback - 1], index - length, w.Low[w.High.Count - lback - 1], Color.FromName(wc));
                if (showClose)
                    Chart.DrawTrendLine("wc", index, w.Close[w.High.Count - lback - 1], index - length, w.Close[w.High.Count - lback - 1], Color.FromName(wc));
                Chart.DrawText("Wh pips", showWLabs ? "W-" + lback + "H\n" + Math.Round((MarketSeries.Close[index] - w.High[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - w.High[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, w.High[w.Low.Count - lback - 1], Color.FromName(wc));
                Chart.DrawText("Wl pips", showWLabs ? "W-" + lback + "L\n" + Math.Round((MarketSeries.Close[index] - w.Low[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - w.Low[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, w.Low[w.Low.Count - lback - 1], Color.FromName(wc));
                if (showClose)
                    Chart.DrawText("Wc pips", showWLabs ? "W-" + lback + "C\n" + Math.Round((MarketSeries.Close[index] - w.Close[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - w.Close[w.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, w.Close[w.Low.Count - lback - 1], Color.FromName(wc));
            }
            if (showD)
            {
                Chart.DrawTrendLine("dh", index, d.High[d.High.Count - lback - 1], index - length, d.High[d.High.Count - lback - 1], Color.FromName(dc));
                Chart.DrawTrendLine("dl", index, d.Low[d.High.Count - lback - 1], index - length, d.Low[d.High.Count - lback - 1], Color.FromName(dc));
                if (showClose)
                    Chart.DrawTrendLine("dc", index, d.Close[d.High.Count - lback - 1], index - length, d.Close[d.High.Count - lback - 1], Color.FromName(wc));
                Chart.DrawText("dh pips", showDLabs ? "D-" + lback + "H\n" + Math.Round((MarketSeries.Close[index] - d.High[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - d.High[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, d.High[d.Low.Count - lback - 1], Color.FromName(dc));
                Chart.DrawText("dl pips", showDLabs ? "D-" + lback + "L\n" + Math.Round((MarketSeries.Close[index] - d.Low[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - d.Low[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, d.Low[d.Low.Count - lback - 1], Color.FromName(dc));
                if (showClose)
                    Chart.DrawText("dc pips", showDLabs ? "D-" + lback + "C\n" + Math.Round((MarketSeries.Close[index] - d.Close[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - d.Close[d.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, d.Close[d.Low.Count - lback - 1], Color.FromName(wc));

            }
            if (showM)
            {
                Chart.DrawTrendLine("mh", index, m.High[m.High.Count - lback - 1], index - length, m.High[m.High.Count - lback - 1], Color.FromName(mc));
                Chart.DrawTrendLine("ml", index, m.Low[m.High.Count - lback - 1], index - length, m.Low[m.High.Count - lback - 1], Color.FromName(mc));
                if (showClose)
                    Chart.DrawTrendLine("mc", index, m.Close[m.High.Count - lback - 1], index - length, m.Close[m.High.Count - lback - 1], Color.FromName(wc));
                Chart.DrawText("mh pips", showMLabs ? "M-" + lback + "H\n" + Math.Round((MarketSeries.Close[index] - m.High[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - m.High[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, m.High[m.Low.Count - lback - 1], Color.FromName(mc));
                Chart.DrawText("ml pips", showMLabs ? "M-" + lback + "L\n" + Math.Round((MarketSeries.Close[index] - m.Low[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - m.Low[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, m.Low[m.Low.Count - lback - 1], Color.FromName(mc));
                if (showClose)
                    Chart.DrawText("mc pips", showWLabs ? "M-" + lback + "C\n" + Math.Round((MarketSeries.Close[index] - m.Close[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString() : Math.Round((MarketSeries.Close[index] - m.Close[m.Low.Count - lback - 1]) / Symbol.PipSize, 2).ToString(), index + 1, m.Close[m.Low.Count - lback - 1], Color.FromName(wc));

            }
        }
    }
}
