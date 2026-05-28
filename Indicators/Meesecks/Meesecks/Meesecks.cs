// cTrader / cAlgo Indicator
// Visualizes Tokyo-anchored VWAP entry zone + micro-structure stop proposals (SNAP vs HUG)
// Attach to 3-minute charts; VWAP computed from 1-minute bars.

using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class TokyoVwapStructureHelper : Indicator
    {
       [Parameter("Show VWAP Dots (M1)", DefaultValue = true)]
public bool ShowVwapDots { get; set; }

[Parameter("VWAP Dots Count (M1)", DefaultValue = 25)]
public int VwapDotsCount { get; set; }

[Parameter("VWAP Dot Length (sec)", DefaultValue = 4)]
public int VwapDotLengthSeconds { get; set; }

[Parameter("VWAP Dot Thickness", DefaultValue = 1)]
public int VwapDotThickness { get; set; }

 // --- VWAP Anchor ---
        [Parameter("Anchor Time Zone", DefaultValue = "Asia/Tokyo")]
        public string AnchorTimeZone { get; set; }

        [Parameter("Anchor Hour (Local TZ)", DefaultValue = 9)]
        public int AnchorHour { get; set; }

        [Parameter("Anchor Minute (Local TZ)", DefaultValue = 0)]
        public int AnchorMinute { get; set; }

        [Parameter("Use TickVolume Weighting", DefaultValue = true)]
        public bool UseTickVolumeWeights { get; set; }

        // --- Entry Zone / Stop Rules ---
        [Parameter("Approach Mult (draw zone when within x band)", DefaultValue = 2.0)]
        public double ApproachMult { get; set; }

        [Parameter("Band = max(spread*mult, minBand) (pips)", DefaultValue = 1.5)]
        public double MinBandPips { get; set; }

        [Parameter("Band Spread Mult", DefaultValue = 1.2)]
        public double BandSpreadMult { get; set; }

        [Parameter("Buffer = max(spread*mult, minBuffer) (pips)", DefaultValue = 1.0)]
        public double MinBufferPips { get; set; }

        [Parameter("Buffer Spread Mult", DefaultValue = 1.5)]
        public double BufferSpreadMult { get; set; }

        [Parameter("HUG Threshold (bars in zone)", DefaultValue = 4)]
        public int HugBarsThreshold { get; set; }

        [Parameter("Max Stop Distance (pips) (skip drawing stop if wider)", DefaultValue = 12.0)]
        public double MaxStopPips { get; set; }

        // --- Visuals ---
        [Parameter("Line Length (bars)", DefaultValue = 2)]
        public int LineLengthBars { get; set; }

        [Parameter("Draw Only Last N Bars", DefaultValue = 300)]
        public int DrawOnlyLastNBars { get; set; }

        [Parameter("Show Labels", DefaultValue = true)]
        public bool ShowLabels { get; set; }

        [Parameter("Show Both Sides (Long+Short)", DefaultValue = true)]
        public bool ShowBothSides { get; set; }

        // --- Internal ---
        private Bars _m1;
        private List<double> _m1Vwap;
        private int _lastComputedM1 = -1;
        private double _cumPV = 0;
        private double _cumV = 0;
        private DateTime _sessionStartUtc = DateTime.MinValue;
        private TimeZoneInfo _anchorTz;

        protected override void Initialize()
        {
            _m1 = MarketData.GetBars(TimeFrame.Minute, SymbolName);
//            _m1 = MarketData.GetBars(TimeFrame.Minute);
            _m1Vwap = new List<double>(_m1.Count + 1024);
            _anchorTz = ResolveTimeZone(AnchorTimeZone);
        }

        public override void Calculate(int index)
        {
            if (index < 1) return;

            int startDraw = Math.Max(0, Bars.Count - DrawOnlyLastNBars);

            // Compute VWAP for this bar time from M1
   //         double vwap = GetVwapAtChartBar(index);
   //         if (double.IsNaN(vwap) || vwap <= 0) return;
if (!TryGetVwapForChartBar(index, out double vwap, out int m1Index))
    return;

            int tfMinutes = GetChartTfMinutes();
            DateTime t1 = Bars.OpenTimes[index];
            DateTime t2 = t1.AddMinutes(Math.Max(1, tfMinutes) * Math.Max(1, LineLengthBars));

            // Spread (best-effort). In cTrader Symbol.Spread is in pips.
            double spreadPips = Math.Max(0.0, Symbol.Spread);

            double bandPips = Math.Max(spreadPips * BandSpreadMult, MinBandPips);
            double bufferPips = Math.Max(spreadPips * BufferSpreadMult, MinBufferPips);

            double bandPx = PipsToPrice(bandPips);
            double bufferPx = PipsToPrice(bufferPips);

            double close = Bars.ClosePrices[index];
            double distPx = Math.Abs(close - vwap);

            bool inApproach = distPx <= (ApproachMult * bandPx);
            bool inZone = distPx <= bandPx;

            if (index >= startDraw && inApproach)
            {
                DrawShortLine($"TVSH_vwap_{index}", t1, vwap, t2, vwap, Color.DodgerBlue, 1, LineStyle.Dots);

                // Entry zone band lines
                DrawShortLine($"TVSH_bandU_{index}", t1, vwap + bandPx, t2, vwap + bandPx, Color.DodgerBlue, 1, LineStyle.DotsVeryRare);
                DrawShortLine($"TVSH_bandL_{index}", t1, vwap - bandPx, t2, vwap - bandPx, Color.DodgerBlue, 1, LineStyle.DotsVeryRare);
            }

            if (!inZone) return;

            // Zone: scan backwards (within a reasonable window) to find contiguous zone segment
            int lookbackMax = 60; // 60 bars on 3m = 3 hours (enough for this style)
            int zoneStart = index;
            for (int j = index; j >= Math.Max(0, index - lookbackMax); j--)
            {
                double vwapJ = GetVwapAtChartBar(j);
                if (double.IsNaN(vwapJ) || vwapJ <= 0) break;

                double distJ = Math.Abs(Bars.ClosePrices[j] - vwapJ);
                if (distJ <= bandPx) zoneStart = j;
                else break;
            }

            int zoneBars = index - zoneStart + 1;
            bool isHug = zoneBars >= HugBarsThreshold;

            // Micro-structure range while in zone
            double zoneLow = double.MaxValue;
            double zoneHigh = double.MinValue;

            for (int j = zoneStart; j <= index; j++)
            {
                zoneLow = Math.Min(zoneLow, Bars.LowPrices[j]);
                zoneHigh = Math.Max(zoneHigh, Bars.HighPrices[j]);
            }

            // Direction guess: where did we come from just before zoneStart?
            int dir = 0; // +1 = from above (long bias), -1 = from below (short bias)
            if (zoneStart > 0)
            {
                double prevClose = Bars.ClosePrices[zoneStart - 1];
                if (prevClose > vwap + bandPx) dir = +1;
                else if (prevClose < vwap - bandPx) dir = -1;
            }
            else
            {
                // fallback: compare current close
                dir = (close >= vwap) ? +1 : -1;
            }

            // Proposed stop(s): zone invalidation + buffer
            // Long stop below zoneLow, short stop above zoneHigh
            double stopLong = zoneLow - bufferPx;
            double stopShort = zoneHigh + bufferPx;

            // Only draw stop if it’s within your “tight stop” philosophy; otherwise it’s probably not a setup you’d take.
            double stopLongDistPips = PriceToPips(close - stopLong);
            double stopShortDistPips = PriceToPips(stopShort - close);

            if (index >= startDraw)
            {
                string regime = isHug ? "HUG" : "SNAP";

                if (ShowBothSides)
                {
                    if (stopLongDistPips > 0 && stopLongDistPips <= MaxStopPips)
                        DrawShortLine($"TVSH_stopL_{index}", t1, stopLong, t2, stopLong, Color.LimeGreen, 2, LineStyle.Solid);

                    if (stopShortDistPips > 0 && stopShortDistPips <= MaxStopPips)
                        DrawShortLine($"TVSH_stopS_{index}", t1, stopShort, t2, stopShort, Color.IndianRed, 2, LineStyle.Solid);
                }
                else
                {
                    // Draw only the “biased” side
                    if (dir == +1 && stopLongDistPips > 0 && stopLongDistPips <= MaxStopPips)
                        DrawShortLine($"TVSH_stopBias_{index}", t1, stopLong, t2, stopLong, Color.LimeGreen, 2, LineStyle.Solid);

                    if (dir == -1 && stopShortDistPips > 0 && stopShortDistPips <= MaxStopPips)
                        DrawShortLine($"TVSH_stopBias_{index}", t1, stopShort, t2, stopShort, Color.IndianRed, 2, LineStyle.Solid);
                }

               if (ShowLabels && index == Bars.Count - 1)
{
    Chart.DrawText("TVSH_lbl_live", $"ZONE {(isHug ? "HUG" : "SNAP")} ({zoneBars})",
        Bars.OpenTimes[index], vwap + (bandPx * 1.3), Color.DodgerBlue);
}
/* if (ShowLabels)
                {
                    // Put label near VWAP (slightly above) so it doesn’t sit on the band lines
                    double labelY = vwap + (bandPx * 1.3);
                    string label = $"ZONE {regime} ({zoneBars})";
                    Chart.DrawText($"TVSH_lbl_{index}", label, t2, labelY, Color.DodgerBlue);
                }
  */
            }
       // Draw last 25-ish VWAP dots (M1) like your monitoring chart
if (ShowVwapDots && index == Bars.Count - 1)
{
    if (m1Index >= 0)
        DrawLastM1VwapDots(m1Index);
}
 }

        // ------------------------
        // VWAP plumbing
        // ------------------------
        private double GetVwapAtChartBar(int chartIndex)
        {
            int tfMinutes = GetChartTfMinutes();

            // Use end-of-bar minute (start time of last minute inside this bar)
            DateTime barEndMinute = Bars.OpenTimes[chartIndex].AddMinutes(Math.Max(1, tfMinutes) - 1);

            int m1Index = GetM1IndexAtOrBefore(barEndMinute);
            if (m1Index < 0) return double.NaN;

            EnsureM1ComputedUpTo(m1Index);
            if (m1Index >= 0 && m1Index < _m1Vwap.Count) return _m1Vwap[m1Index];

            return double.NaN;
        }

private bool TryGetVwapForChartBar(int chartIndex, out double vwap, out int m1Index)
{
    vwap = double.NaN;
    m1Index = -1;

    int tfMinutes = GetChartTfMinutes();
    DateTime barEndMinute = Bars.OpenTimes[chartIndex].AddMinutes(Math.Max(1, tfMinutes) - 1);

    m1Index = GetM1IndexAtOrBefore(barEndMinute);
    if (m1Index < 0)
        return false;

    EnsureM1ComputedUpTo(m1Index);

    if (m1Index >= 0 && m1Index < _m1Vwap.Count)
    {
        vwap = _m1Vwap[m1Index];
        return !(double.IsNaN(vwap) || vwap <= 0);
    }

    return false;
}
private void DrawLastM1VwapDots(int m1EndIndex)
{
    int count = Math.Max(1, VwapDotsCount);
    int start = Math.Max(0, m1EndIndex - count + 1);

    int sec = Math.Max(1, VwapDotLengthSeconds);
    int th = Math.Max(1, VwapDotThickness);

    for (int i = start; i <= m1EndIndex; i++)
    {
        double y = _m1Vwap[i];
        if (double.IsNaN(y) || y <= 0) continue;

        var t1 = _m1.OpenTimes[i];
        var t2 = t1.AddSeconds(sec);

        string name = $"TVSH_m1dot_{i}";

        // Tiny "dot" as a micro-dash (much smaller than DrawIcon circles)
        Chart.DrawTrendLine(name, t1, y, t2, y, Color.White, th, LineStyle.Solid);
    }

    // Cleanup: remove a small tail so old dots don’t remain
    for (int i = start - 20; i < start; i++)
        if (i >= 0) Chart.RemoveObject($"TVSH_m1dot_{i}");
}

        private void EnsureM1ComputedUpTo(int m1Index)
        {
            if (m1Index <= _lastComputedM1) return;

            // Make sure list capacity
            while (_m1Vwap.Count < _m1.Count) _m1Vwap.Add(double.NaN);

            for (int i = _lastComputedM1 + 1; i <= m1Index; i++)
            {
                DateTime tUtc = EnsureUtc(_m1.OpenTimes[i]);
                DateTime sessStartUtc = GetSessionStartUtc(tUtc);

                if (i == 0 || sessStartUtc != _sessionStartUtc)
                {
                    _sessionStartUtc = sessStartUtc;
                    _cumPV = 0;
                    _cumV = 0;
                }

                double tp = (_m1.HighPrices[i] + _m1.LowPrices[i] + _m1.ClosePrices[i]) / 3.0;

                double w = 1.0;
                if (UseTickVolumeWeights)
                    w = Math.Max(1.0, _m1.TickVolumes[i]);

                _cumPV += tp * w;
                _cumV += w;

                _m1Vwap[i] = _cumPV / _cumV;
            }

            _lastComputedM1 = m1Index;
        }

        private DateTime GetSessionStartUtc(DateTime barTimeUtc)
        {
            // Convert UTC -> anchor TZ, snap to today’s anchor time, if bar is before anchor -> previous day
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(barTimeUtc, _anchorTz);

            DateTime anchorLocal = new DateTime(local.Year, local.Month, local.Day, AnchorHour, AnchorMinute, 0, DateTimeKind.Unspecified);

            if (local < anchorLocal)
                anchorLocal = anchorLocal.AddDays(-1);

            // Convert anchor local -> UTC
            DateTime anchorUtc = TimeZoneInfo.ConvertTimeToUtc(anchorLocal, _anchorTz);
            return anchorUtc;
        }

        private int GetM1IndexAtOrBefore(DateTime t)
        {
            // Try exact match first
            int idx = _m1.OpenTimes.GetIndexByTime(t);
            if (idx >= 0) return idx;

            // Fallback: walk back minute-by-minute a few times (covers tiny alignment oddities)
            DateTime tt = t;
            for (int k = 0; k < 5; k++)
            {
                tt = tt.AddMinutes(-1);
                idx = _m1.OpenTimes.GetIndexByTime(tt);
                if (idx >= 0) return idx;
            }

            // As last resort: return last bar if t is in the future
            if (t >= _m1.OpenTimes[_m1.Count - 1])
                return _m1.Count - 1;

            return -1;
        }

private int GetChartTfMinutes()
{
    if (Bars.Count < 2)
        return 1;

    var minutes = (Bars.OpenTimes[1] - Bars.OpenTimes[0]).TotalMinutes;
    return Math.Max(1, (int)Math.Round(minutes));
}


        // ------------------------
        // Drawing helpers
        // ------------------------
        private void DrawShortLine(string name, DateTime x1, double y1, DateTime x2, double y2, Color color, int thickness, LineStyle style)
        {
            // DrawTrendLine replaces if same name exists
            Chart.DrawTrendLine(name, x1, y1, x2, y2, color, thickness, style);
        }

        private double PipsToPrice(double pips) => pips * Symbol.PipSize;
        private double PriceToPips(double priceDiff) => priceDiff / Symbol.PipSize;

        private static DateTime EnsureUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc) return dt;
            // cTrader times are typically UTC already, but be safe
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        private static TimeZoneInfo ResolveTimeZone(string id)
        {
            // Try the provided ID; fallback to common Windows/IANA mapping for Tokyo.
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }

            // Common fallback mapping
            if (string.Equals(id, "Asia/Tokyo", StringComparison.OrdinalIgnoreCase))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"); } catch { }
            }

            // Final fallback
            return TimeZoneInfo.Utc;
        }
    }
}

