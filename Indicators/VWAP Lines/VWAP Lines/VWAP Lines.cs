using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VWAPLines : Indicator
    {
        [Parameter("Show", Group = "Daily", DefaultValue = true)]
        public bool ShowDaily { get; set; }

        [Parameter("Max Lines", Group = "Daily", DefaultValue = 0)]
        public int DailyMaxLines { get; set; }

        [Parameter("Line Color", Group = "Daily", DefaultValue = "ff000000")]
        public string DailyColor { get; set; }

        [Parameter("Line Style", Group = "Daily", DefaultValue = LineStyle.Solid)]
        public LineStyle DailyStyle { get; set; }

        [Parameter("Line Thickness", Group = "Daily", DefaultValue = 1, MinValue = 0)]
        public int DailyThickness { get; set; }

        [Parameter("Show", Group = "Weekly", DefaultValue = true)]
        public bool ShowWeekly { get; set; }

        [Parameter("Max Lines", Group = "Weekly", DefaultValue = 0)]
        public int WeeklyMaxLines { get; set; }

        [Parameter("Line Color", Group = "Weekly", DefaultValue = "ffff0000")]
        public string WeeklyColor { get; set; }

        [Parameter("Line Style", Group = "Weekly", DefaultValue = LineStyle.Solid)]
        public LineStyle WeeklyStyle { get; set; }

        [Parameter("Line Thickness", Group = "Weekly", DefaultValue = 1, MinValue = 0)]
        public int WeeklyThickness { get; set; }

        [Parameter("Show", Group = "Monthly", DefaultValue = true)]
        public bool ShowMonthly { get; set; }

        [Parameter("Max Lines", Group = "Monthly", DefaultValue = 0)]
        public int MonthlyMaxLines { get; set; }

        [Parameter("Line Color", Group = "Monthly", DefaultValue = "ffffff00")]
        public string MonthlyColor { get; set; }

        [Parameter("Line Style", Group = "Monthly", DefaultValue = LineStyle.Solid)]
        public LineStyle MonthlyStyle { get; set; }

        [Parameter("Line Thickness", Group = "Monthly", DefaultValue = 1, MinValue = 0)]
        public int MonthlyThickness { get; set; }

        private VWAP _daily, _weekly, _monthly;

        protected override void Initialize()
        {
            if (ShowDaily)
                _daily = Indicators.GetIndicator<VWAP>(VWAP.PeriodType.Daily, DailyColor, DailyStyle, DailyThickness, DailyMaxLines);

            if (ShowWeekly)
                _weekly = Indicators.GetIndicator<VWAP>(VWAP.PeriodType.Weekly, WeeklyColor, WeeklyStyle, WeeklyThickness, WeeklyMaxLines);

            if (ShowMonthly)
                _monthly = Indicators.GetIndicator<VWAP>(VWAP.PeriodType.Monthly, MonthlyColor, MonthlyStyle, MonthlyThickness, MonthlyMaxLines);
        }

        public override void Calculate(int index)
        {
            var _ = 0.0;

            if (ShowDaily)
                _ = _daily.Result.LastValue;

            if (ShowWeekly)
                _ = _weekly.Result.LastValue;

            if (ShowMonthly)
                _ = _monthly.Result.LastValue;
        }
    }
}
