using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Levels(0.0, 2.0)]
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZScore : Indicator
    {
        private MovingAverage movingAverage;
        private StandardDeviation standardDeviation;

        [Parameter()]
        public DataSeries Source { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }


        protected override void Initialize()
        {
            // Initialize and create nested indicators
            movingAverage = Indicators.MovingAverage(Source, 14, MovingAverageType.Simple);
            standardDeviation = Indicators.StandardDeviation(Source, 14, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = (Source[index] - movingAverage.Result[index]) / standardDeviation.Result[index];
            if (IsLastBar)
            {
                Print(((Source[index] - movingAverage.Result[index]) / standardDeviation.Result[index]) * 50);
                Print(((Source[index] - movingAverage.Result[index]) / standardDeviation.Result[index]));
            }
        }
    }
}
