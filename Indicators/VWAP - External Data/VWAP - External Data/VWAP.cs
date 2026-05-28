using System;
using cAlgo.API;

namespace cAlgo
{
    public class VWAP : Indicator
    {
        [Parameter("Period Type", DefaultValue = PeriodType.Daily)]
        public PeriodType Type { get; set; }

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

        public bool IsFirstBarOfSession(int index)
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
    }
}
