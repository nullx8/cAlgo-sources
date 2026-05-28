using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RealPivot : Indicator
    {
        [Parameter("Pivot TF", Group = "Pivots", DefaultValue = PivotTF.Daily)]
        public PivotTF TF { get; set; }
        [Parameter("Pivot Type", Group = "Pivots", DefaultValue = PivotType.Camarilla)]
        public PivotType Type { get; set; }

        [Parameter("Resistances Color", Group = "Style", DefaultValue = "Red")]
        public string RColor { get; set; }
        [Parameter("Supports Color", Group = "Style", DefaultValue = "Lime")]
        public string SColor { get; set; }
        [Parameter("Opacity", Group = "Style", DefaultValue = 100, MaxValue = 100, MinValue = 0)]
        public int Opacity { get; set; }
        [Parameter("Thickness", Group = "Style", DefaultValue = 1, MaxValue = 5, MinValue = 0)]
        public int Thickness { get; set; }

        [Parameter("Draw Labels", Group = "Labels", DefaultValue = true)]
        public bool Labels { get; set; }

        [Parameter("Draw 1s", Group = "Drawing Settings", DefaultValue = true)]
        public bool Draw1s { get; set; }
        [Parameter("Draw 2s", Group = "Drawing Settings", DefaultValue = true)]
        public bool Draw2s { get; set; }
        [Parameter("Draw 3s", Group = "Drawing Settings", DefaultValue = true)]
        public bool Draw3s { get; set; }

        [Parameter("Draw 2-3 Area", Group = "Drawing Settings", DefaultValue = false)]
        public bool DrawArea { get; set; }

        [Parameter("Force Pivot on Close", Group = "Additional Features", DefaultValue = false)]
        public bool PivotOnClose { get; set; }

        public enum PivotTF
        {
            Hour,
            Hour4,
            Hour12,
            Daily,
            Weekly,
            Monthly
        }

        public enum PivotType
        {
            Standard,
            Fibonacci,
            Camarilla,
            Woodie,
            Demark
        }

        private Color SupportColor, ResistanceColor;
        private MarketSeries Series;

        protected override void Initialize()
        {
            Opacity = (int)(255 * 0.01 * Opacity);
            SupportColor = Color.FromArgb(Opacity, Color.FromName(SColor).R, Color.FromName(SColor).G, Color.FromName(SColor).B);
            ResistanceColor = Color.FromArgb(Opacity, Color.FromName(RColor).R, Color.FromName(RColor).G, Color.FromName(RColor).B);
            Series = TF == PivotTF.Hour ? MarketData.GetSeries(TimeFrame.Hour) : TF == PivotTF.Hour4 ? MarketData.GetSeries(TimeFrame.Hour4) : TF == PivotTF.Hour12 ? MarketData.GetSeries(TimeFrame.Hour12) : TF == PivotTF.Daily ? MarketData.GetSeries(TimeFrame.Daily) : TF == PivotTF.Weekly ? MarketData.GetSeries(TimeFrame.Weekly) : MarketData.GetSeries(TimeFrame.Monthly);
        }

        public override void Calculate(int index)
        {
            if (CheckError())
            {
                Chart.DrawStaticText("Error", "YOU DON'T WANT THIS, CHANGE PIVOT'S TIMEFRAME", VerticalAlignment.Center, HorizontalAlignment.Center, Color.Red);
                return;
            }

            int index2 = Series.OpenTime.GetIndexByTime(MarketSeries.OpenTime[index]);
            int candleColor = (int)(Math.Abs(Series.Close[index2 - 1] - Series.Open[index2 - 1]) / (Series.Close[index2 - 1] - Series.Open[index2 - 1]));
            double pivot = Type == PivotType.Woodie ? (Series.High[index2 - 1] + Series.Low[index2 - 1] + 2 * Series.Open[index2]) / 4 : Type == PivotType.Demark ? (candleColor == 1 ? Series.High[index2 - 1] * 2 + Series.Low[index2 - 1] + Series.Close[index2 - 1] : candleColor == -1 ? Series.High[index2 - 1] + Series.Low[index2 - 1] * 2 + Series.Close[index2 - 1] : Series.High[index2 - 1] + Series.Low[index2 - 1] + Series.Close[index2 - 1] * 2) : (Series.High[index2 - 1] + Series.Low[index2 - 1] + Series.Close[index2 - 1]) / 3;
            if (PivotOnClose)
                pivot = Series.Close[index2 - 1];
            double range = Series.High[index2 - 1] - Series.Low[index2 - 1];
            double s1 = Type == PivotType.Standard ? 2 * pivot - Series.High[index2 - 1] : Type == PivotType.Fibonacci ? pivot - range * 0.382 : Type == PivotType.Camarilla ? Series.Close[index2 - 1] - 0.0916 * range : Type == PivotType.Woodie ? 2 * pivot - Series.High[index2 - 1] : Type == PivotType.Demark ? pivot / 2 - Series.High[index2 - 1] : 0;
            double r1 = Type == PivotType.Standard ? 2 * pivot - Series.Low[index2 - 1] : Type == PivotType.Fibonacci ? pivot + range * 0.382 : Type == PivotType.Camarilla ? Series.Close[index2 - 1] + 0.0916 * range : Type == PivotType.Woodie ? 2 * pivot - Series.Low[index2 - 1] : Type == PivotType.Demark ? pivot / 2 - Series.Low[index2 - 1] : 0;
            double s2 = Type == PivotType.Standard ? pivot - range : Type == PivotType.Fibonacci ? pivot - range * 0.618 : Type == PivotType.Camarilla ? Series.Close[index2 - 1] - 0.183 * range : Type == PivotType.Woodie ? pivot - range : 0;
            double r2 = Type == PivotType.Standard ? pivot + range : Type == PivotType.Fibonacci ? pivot + range * 0.618 : Type == PivotType.Camarilla ? Series.Close[index2 - 1] + 0.183 * range : Type == PivotType.Woodie ? pivot + range : 0;
            double s3 = Type == PivotType.Standard ? s1 - range : Type == PivotType.Fibonacci ? pivot - range : Type == PivotType.Camarilla ? Series.Close[index2 - 1] - 0.275 * range : Type == PivotType.Woodie ? Series.Low[index2 - 1] - 2 * (Series.High[index2 - 1] - pivot) : 0;
            double r3 = Type == PivotType.Standard ? r1 + range : Type == PivotType.Fibonacci ? pivot + range : Type == PivotType.Camarilla ? Series.Close[index2 - 1] + 0.275 * range : Type == PivotType.Woodie ? Series.High[index2 - 1] + 2 * (pivot - Series.Low[index2 - 1]) : 0;
            double s4 = Type == PivotType.Woodie ? s3 - range : Series.Close[index2 - 1] - 0.55 * range;
            double r4 = Type == PivotType.Woodie ? r3 + range : Series.Close[index2 - 1] + 0.55 * range;

            DateTime OpenTime = Series.OpenTime[index2];
            DateTime EndTime = TF == PivotTF.Hour ? OpenTime.AddHours(1) : TF == PivotTF.Hour4 ? OpenTime.AddHours(4) : TF == PivotTF.Hour12 ? OpenTime.AddHours(12) : TF == PivotTF.Daily ? OpenTime.AddDays(1) : TF == PivotTF.Weekly ? OpenTime.AddDays(7) : OpenTime.AddMonths(1);

            pivot /= Type == PivotType.Demark ? 4 : 1;

            Chart.DrawTrendLine("Pivot " + OpenTime, OpenTime, pivot, EndTime, pivot, Color.FromArgb(Opacity, 255, 255, 255), Thickness, LineStyle.Solid);
            if (Draw1s)
                Chart.DrawTrendLine("S1 " + OpenTime, OpenTime, s1, EndTime, s1, SupportColor, Thickness, LineStyle.Dots);
            if (Draw1s)
                Chart.DrawTrendLine("R1 " + OpenTime, OpenTime, r1, EndTime, r1, ResistanceColor, Thickness, LineStyle.Dots);
            if (Type != PivotType.Demark)
            {
                if (Draw2s)
                    Chart.DrawTrendLine("S2 " + OpenTime, OpenTime, s2, EndTime, s2, SupportColor, Thickness, LineStyle.DotsRare);
                if (Draw3s)
                    Chart.DrawTrendLine("S3 " + OpenTime, OpenTime, s3, EndTime, s3, SupportColor, Thickness, LineStyle.DotsVeryRare);
                if (Draw2s)
                    Chart.DrawTrendLine("R2 " + OpenTime, OpenTime, r2, EndTime, r2, ResistanceColor, Thickness, LineStyle.DotsRare);
                if (Draw3s)
                    Chart.DrawTrendLine("R3 " + OpenTime, OpenTime, r3, EndTime, r3, ResistanceColor, Thickness, LineStyle.DotsVeryRare);
            }
            if (Type == PivotType.Camarilla || Type == PivotType.Woodie)
            {
                Chart.DrawTrendLine("R4 " + OpenTime, OpenTime, r4, EndTime, r4, ResistanceColor, Thickness, LineStyle.DotsVeryRare);
                Chart.DrawTrendLine("S4 " + OpenTime, OpenTime, s4, EndTime, s4, SupportColor, Thickness, LineStyle.DotsVeryRare);
            }

            if (DrawArea)
            {
                Chart.DrawRectangle("R Area " + OpenTime, OpenTime, r2, EndTime, r3, Color.FromArgb(50, ResistanceColor.R, ResistanceColor.G, ResistanceColor.B)).IsFilled = true;
                Chart.DrawRectangle("S Area " + OpenTime, OpenTime, s2, EndTime, s3, Color.FromArgb(50, SupportColor.R, SupportColor.G, SupportColor.B)).IsFilled = true;
            }

            if (Labels)
            {
                string tf = " - ";
                switch (TF)
                {
                    case PivotTF.Hour:
                        tf += "H1";
                        break;
                    case PivotTF.Hour4:
                        tf += "H4";
                        break;
                    case PivotTF.Hour12:
                        tf += "H12";
                        break;
                    case PivotTF.Daily:
                        tf += "D";
                        break;
                    case PivotTF.Weekly:
                        tf += "W";
                        break;
                    case PivotTF.Monthly:
                        tf += "M";
                        break;
                }
                Chart.DrawText("Label P " + OpenTime, "P - " + Math.Round(pivot, Symbol.Digits) + tf, OpenTime, pivot, Color.FromArgb(Opacity, 255, 255, 255));
                if (Draw1s)
                    Chart.DrawText("Label S1 " + OpenTime, "S1 - " + Math.Round(s1, Symbol.Digits) + tf, OpenTime, s1, SupportColor);
                if (Draw1s)
                    Chart.DrawText("Label R1 " + OpenTime, "R1 - " + Math.Round(r1, Symbol.Digits) + tf, OpenTime, r1, ResistanceColor);
                if (Type != PivotType.Demark)
                {
                    if (Draw2s)
                        Chart.DrawText("Label S2 " + OpenTime, "S2 - " + Math.Round(s2, Symbol.Digits) + tf, OpenTime, s2, SupportColor);
                    if (Draw3s)
                        Chart.DrawText("Label S3 " + OpenTime, "S3 - " + Math.Round(s3, Symbol.Digits) + tf, OpenTime, s3, SupportColor);
                    if (Draw2s)
                        Chart.DrawText("Label R2 " + OpenTime, "R2 - " + Math.Round(r2, Symbol.Digits) + tf, OpenTime, r2, ResistanceColor);
                    if (Draw3s)
                        Chart.DrawText("Label R3 " + OpenTime, "R3 - " + Math.Round(r3, Symbol.Digits) + tf, OpenTime, r3, ResistanceColor);
                }
                if (Type == PivotType.Camarilla || Type == PivotType.Woodie)
                {
                    Chart.DrawText("Label S4 " + OpenTime, "S4 - " + Math.Round(s4, Symbol.Digits) + tf, OpenTime, s4, SupportColor);
                    Chart.DrawText("Label R4 " + OpenTime, "R4 - " + Math.Round(r4, Symbol.Digits) + tf, OpenTime, r4, ResistanceColor);
                }
            }
        }

        private bool CheckError()
        {
            return TF == PivotTF.Hour && TimeFrame >= TimeFrame.Hour ? true : TF == PivotTF.Hour4 && TimeFrame >= TimeFrame.Hour4 ? true : TF == PivotTF.Daily && TimeFrame >= TimeFrame.Daily ? true : TF == PivotTF.Weekly && TimeFrame >= TimeFrame.Weekly ? true : false;
        }

    }
}
