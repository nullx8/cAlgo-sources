//  VWAP (Volume-Weighted Average Price) v1.0 by VFX
//  https://ctrader.com/algos/cbots/show/

//  Version history:
//  v1.0    - initial release


using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AutoRescale = false, TimeZone = TimeZones.CentralEuropeanStandardTime, AccessRights = AccessRights.None)]
    public class VWAP : Indicator
    {
        [Parameter("Time Period", Group = "🛠 General", DefaultValue = vwapTimeFrame.Daily)]
        public vwapTimeFrame parTF { get; set; }
        [Parameter("Thime Shift (Hours)", Group = "🛠 General", DefaultValue = 0, MinValue = -12, MaxValue = 12)]
        public int parHoursShift { get; set; }
        [Parameter("Use Typical Price", Group = "🛠 General", DefaultValue = true)]
        public bool parUseTypicalPrice { get; set; }
        [Parameter("Show RSI Arrow", Group = "🛠 General", DefaultValue = true)]
        public bool parShowRSIArrow { get; set; }

        [Parameter("Show StDevs", Group = "📈 StDev Lines", DefaultValue = false)]
        public bool parShowSTDs { get; set; }
        [Parameter("Multiplier 1", Group = "📈 StDev Lines", DefaultValue = 1)]
        public double parSTD1M { get; set; }
        [Parameter("Multiplier 2", Group = "📈 StDev Lines", DefaultValue = 2)]
        public double parSTD2M { get; set; }
        [Parameter("Multiplier 3", Group = "📈 StDev Lines", DefaultValue = 3)]
        public double parSTD3M { get; set; }

        [Output("Main", LineColor = "Orange", Thickness = 3, PlotType = PlotType.Points)]
        public IndicatorDataSeries Result { get; set; }
        [Output("Previous Limit", LineColor = "Orange", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries Previous { get; set; }
        [Output("Upper Dev 1", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD1U { get; set; }
        [Output("Lower Dev 1", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD1D { get; set; }
        [Output("Upper Dev 2", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD2U { get; set; }
        [Output("Lower Dev 2", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD2D { get; set; }
        [Output("Upper Dev 3", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD3U { get; set; }
        [Output("Lower Dev 3", LineColor = "SkyBlue", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries STD3D { get; set; }

        private bool isCurrentData, isFullData;
        private int startIndex, prevStartIndex, prevIndex;
        private double cptv, cv, err, prevResult;
        private Bars BarsTF;
        private RelativeStrengthIndex rsi;

        public enum vwapTimeFrame
        {
            Daily,
            Weekly,
            Monthly,
            Yearly
        }

        protected override void Initialize()
        {
            isCurrentData = false;
            isFullData = false;

            prevIndex = 0;
            startIndex = 0;
            prevStartIndex = -1;
            cptv = 0;
            cv = 0;
            err = 0;
            prevResult = double.NaN;

            if (parShowRSIArrow)
                rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);

            BarsTF = MarketData.GetBars(TimeFrame.Daily);
            DateTime dayStartTime = BarsTF.OpenTimes[BarsTF.OpenTimes.GetIndexByTime(Bars.OpenTimes.LastValue.AddHours(-(double)parHoursShift))].AddHours((double)parHoursShift);
            DateTime currentStartDate = (parTF == vwapTimeFrame.Yearly ? new DateTime(dayStartTime.Year, 1, 1) : (parTF == vwapTimeFrame.Monthly ? new DateTime(dayStartTime.Year, dayStartTime.Month, 1) : (parTF == vwapTimeFrame.Weekly ? dayStartTime.AddDays(-(((int)dayStartTime.DayOfWeek + 6) % 7)) : dayStartTime))).AddHours(4).Date;
            DateTime fullStartDate = (parTF == vwapTimeFrame.Yearly ? currentStartDate.AddYears(-1) : (parTF == vwapTimeFrame.Monthly ? currentStartDate.AddMonths(-1) : (parTF == vwapTimeFrame.Weekly ? currentStartDate.AddDays(-7) : currentStartDate.AddDays(-1))));

            if (Bars.OpenTimes[0] <= currentStartDate)
                isCurrentData = true;
            if (Bars.OpenTimes[0] <= fullStartDate)
                isFullData = true;
        }

        public override void Calculate(int index)
        {
            if (!isCurrentData)
                return;

            if (index > prevIndex)
            {
                DateTime dayStartTime = BarsTF.OpenTimes[BarsTF.OpenTimes.GetIndexByTime(Bars.OpenTimes[index].AddHours(-(double)parHoursShift))].AddHours((double)parHoursShift);
                DateTime dayDate = dayStartTime.AddHours(4).Date;
                int elapsedDays = (parTF == vwapTimeFrame.Yearly ? dayDate.DayOfYear : (parTF == vwapTimeFrame.Monthly ? dayDate.Day - 1 : (parTF == vwapTimeFrame.Weekly ? ((int)dayDate.DayOfWeek + 6) % 7 : 0)));
                startIndex = Bars.OpenTimes.GetIndexByTime(dayStartTime.AddDays(-elapsedDays));

                // calculate the final version of the previous point
                cptv += Bars.TypicalPrices[index - 1] * Bars.TickVolumes[index - 1];
                cv += Bars.TickVolumes[index - 1];
                if (cv == 0)
                    Print("cv = 0");
                Result[index - 1] = cptv / cv;

                // Standard Deviation
                if (parShowSTDs)
                {
                    err += Math.Pow((parUseTypicalPrice ? Bars.TypicalPrices[index - 1] : Bars.ClosePrices[index - 1]) - Result[index - 1], 2);
                    double squaredErrors = Math.Sqrt(err / (index - 1 - startIndex + 1));

                    STD1U[index - 1] = squaredErrors * parSTD1M + Result[index - 1];
                    STD2U[index - 1] = squaredErrors * parSTD2M + Result[index - 1];
                    STD3U[index - 1] = squaredErrors * parSTD3M + Result[index - 1];
                    STD1D[index - 1] = Result[index - 1] - squaredErrors * parSTD1M;
                    STD2D[index - 1] = Result[index - 1] - squaredErrors * parSTD2M;
                    STD3D[index - 1] = Result[index - 1] - squaredErrors * parSTD3M;
                }

                // New Bar on the higher TF = new cycle
                if (startIndex > prevStartIndex)
                {
                    prevResult = Result[index - 1];
                    prevStartIndex = startIndex;
                    cptv = 0;
                    cv = 0;
                    err = 0;
                }

                if (isFullData)
                {
                    Previous[index] = prevResult;
                    ChartText text = Chart.DrawText("PrevVWAP", " last " + parTF.ToString().ToUpper().Substring(0, 1), index, prevResult, Color.FromArgb(192, Color.Orange));
                    text.VerticalAlignment = VerticalAlignment.Center;
                    text.HorizontalAlignment = HorizontalAlignment.Right;
                }
                prevIndex = index;
            }

            if (IsLastBar)
            {
                // last point, arrows, and stDev bands (redrawn on every tick)
                Result[index] = (cptv + (parUseTypicalPrice ? Bars.TypicalPrices[index] : Bars.ClosePrices[index]) * Bars.TickVolumes[index]) / (cv + Bars.TickVolumes[index]);

                // RSI Arrow
                if (parShowRSIArrow)
                {
                    string rsiArrow = rsi.Result.IsRising() ? "▲" : (rsi.Result.IsFalling() ? "▼" : "►");
                    Color rsiAColor = Color.FromArgb(192, rsi.Result.IsRising() ? Color.LimeGreen : (rsi.Result.IsFalling() ? Color.Crimson : Color.Orange));
                    Color rsiRColor = Color.FromArgb(192, Color.Orange);
                    if (rsi.Result[index] >= 55 && rsi.Result[index] <= 70)
                        rsiRColor = Color.FromArgb(192, Color.LimeGreen);
                    else if (rsi.Result[index] <= 45 && rsi.Result[index] >= 30)
                        rsiRColor = Color.FromArgb(192, Color.Crimson);

                    Chart.DrawText("RSIBackground", new string((char)9608, 5), index, Result[index], Color.FromArgb(128, Color.Black)).VerticalAlignment = VerticalAlignment.Center;
                    Chart.DrawText("RSIArrow", new string((char)160, 1) + rsiArrow, index, Result[index], rsiAColor).VerticalAlignment = VerticalAlignment.Center;
                    Chart.DrawText("RSIResult", new string((char)160, 5) + rsi.Result[index].ToString("F1"), index, Result[index], rsiRColor).VerticalAlignment = VerticalAlignment.Center;
                }
                else
                    Chart.DrawText("VWAPType", new string((char)160, 1) + parTF.ToString().ToUpper().Substring(0, 1), index, Result[index], Color.FromArgb(192, Color.Orange)).VerticalAlignment = VerticalAlignment.Center;

                // Standard Deviation
                if (parShowSTDs)
                {
                    double squaredErrors = Math.Sqrt((err + Math.Pow((parUseTypicalPrice ? Bars.TypicalPrices[index] : Bars.ClosePrices[index]) - Result[index], 2)) / (index - startIndex + 1));
                    STD1U[index] = squaredErrors * parSTD1M + Result[index];
                    STD2U[index] = squaredErrors * parSTD2M + Result[index];
                    STD3U[index] = squaredErrors * parSTD3M + Result[index];
                    STD1D[index] = Result[index] - squaredErrors * parSTD1M;
                    STD2D[index] = Result[index] - squaredErrors * parSTD2M;
                    STD3D[index] = Result[index] - squaredErrors * parSTD3M;
                }
            }
        }
    }
}
