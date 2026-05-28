using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TWAP : Indicator
    {
        [Parameter(DefaultValue = 0.0)]
        public double Parameter { get; set; }

        [Output("Main", PlotType = PlotType.Points, Thickness = 2, Color = Colors.Yellow)]
        public IndicatorDataSeries Result { get; set; }

        private IndicatorDataSeries _cumulativeTime;
        private IndicatorDataSeries _cumulativeTypicalTime;

        protected override void Initialize()
        {
            _cumulativeTime = CreateDataSeries();
            _cumulativeTypicalTime = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            var barDuration = (IsLastBar ? Time : MarketSeries.OpenTime[index + 1]).Subtract(MarketSeries.OpenTime[index]).TotalSeconds;
            var isFirstBarOfSession = MarketSeries.OpenTime[index].Date > MarketSeries.OpenTime[index - 1].Date;
            if (isFirstBarOfSession)
            {
                _cumulativeTime[index] = barDuration;
                _cumulativeTypicalTime[index] = barDuration * MarketSeries.Typical[index];
            }
            else
            {
                _cumulativeTime[index] = _cumulativeTime[index - 1] + barDuration;
                _cumulativeTypicalTime[index] = _cumulativeTypicalTime[index - 1] + barDuration * MarketSeries.Typical[index];
            }

            Result[index] = _cumulativeTypicalTime[index] / _cumulativeTime[index];
        }
    }
}
