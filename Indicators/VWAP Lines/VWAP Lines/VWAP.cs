using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public class VWAP : Indicator
    {
        [Parameter]
        public PeriodType Type { get; set; }

        [Parameter]
        public string LineColor { get; set; }

        [Parameter]
        public LineStyle LineStyle { get; set; }

        [Parameter]
        public int LineThickness { get; set; }

        [Parameter]
        public int MaxLines { get; set; }

        [Output("Result")]
        public IndicatorDataSeries Result { get; set; }

        public enum PeriodType
        {
            Daily,
            Weekly,
            Monthly
        }

        private IndicatorDataSeries _cumulativeVolume;
        private IndicatorDataSeries _cumulativeTypicalVolume;
        private readonly List<ChartTrendLine> _lines = new List<ChartTrendLine>();

        protected override void Initialize()
        {
            _cumulativeVolume = CreateDataSeries();
            _cumulativeTypicalVolume = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            var barVolume = MarketSeries.TickVolume[index];
            var isFirstBarOfSession = IsFirstBarOfSession(index);
            if (isFirstBarOfSession)
            {
                DrawLine(index);

                _cumulativeVolume[index] = barVolume;
                _cumulativeTypicalVolume[index] = barVolume * MarketSeries.Typical[index];
            }
            else
            {
                _cumulativeVolume[index] = _cumulativeVolume[index - 1] + barVolume;
                _cumulativeTypicalVolume[index] = _cumulativeTypicalVolume[index - 1] + barVolume * MarketSeries.Typical[index];
            }

            Result[index] = _cumulativeTypicalVolume[index] / _cumulativeVolume[index];
        }

        private bool IsFirstBarOfSession(int index)
        {
            var time = MarketSeries.OpenTime;

            switch (Type)
            {
                case PeriodType.Daily:
                    return time[index].Date > time[index - 1].Date;
                case PeriodType.Weekly:
                    return time[index].DayOfWeek == DayOfWeek.Monday && time[index - 1].DayOfWeek != DayOfWeek.Monday;
                case PeriodType.Monthly:
                    return time[index].Month != time[index - 1].Month;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawLine(int index)
        {
            var y = Result[index - 1];
            var time = MarketSeries.OpenTime[index];
            var line = Chart.DrawTrendLine(string.Format("{0} - {1}", Type, time), time, y, time.AddMinutes(1), y, LineColor, LineThickness, LineStyle);
            line.ExtendToInfinity = true;

            _lines.Add(line);

            if (MaxLines > 0)
            {
                while (_lines.Count > MaxLines)
                {
                    Chart.RemoveObject(_lines[0].Name);
                    _lines.RemoveAt(0);
                }
            }
        }
    }
}