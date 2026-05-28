using System;
using cAlgo.API;

namespace cAlgo
{
    /// <summary>
    /// Volume Weighted Average Price
    /// </summary>
    public class VWAP : Indicator
    {
        [Parameter("Deviation", DefaultValue = 3)]
        public double Deviation { get; set; }

        [Output("Result")]
        public IndicatorDataSeries Result { get; set; }

        [Output("Upper", PlotType = PlotType.Points)]
        public IndicatorDataSeries Upper { get; set; }

        [Output("Lower", PlotType = PlotType.Points)]
        public IndicatorDataSeries Lower { get; set; }

        private IndicatorDataSeries _cumulativeVolume;
        private IndicatorDataSeries _cumulativeTypicalVolume;
        private IndicatorDataSeries _deviation;
        private int _startIndex;

        protected override void Initialize()
        {
            _cumulativeVolume = CreateDataSeries();
            _cumulativeTypicalVolume = CreateDataSeries();
            _deviation = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            var barVolume = MarketSeries.TickVolume[index];
            var isFirstBarOfSession = MarketSeries.OpenTime[index].Date > MarketSeries.OpenTime[index - 1].Date;
            if (isFirstBarOfSession)
            {
                _cumulativeVolume[index] = barVolume;
                _cumulativeTypicalVolume[index] = barVolume * MarketSeries.Typical[index];
                _deviation[index] = 0;
                _startIndex = index;
            }
            else
            {
                _cumulativeVolume[index] = _cumulativeVolume[index - 1] + barVolume;
                _cumulativeTypicalVolume[index] = _cumulativeTypicalVolume[index - 1] + barVolume * MarketSeries.Typical[index];
            }

            Result[index] = _cumulativeTypicalVolume[index] / _cumulativeVolume[index];

            if (isFirstBarOfSession)
            {
                Upper[index] = Result[index];
                Lower[index] = Result[index];
            }
            else
            {
                var sd = 0.0;
                for (var i = _startIndex; i <= index; i++)
                {
                    var diff = MarketSeries.Typical[i] - Result[index];
                    sd += MarketSeries.TickVolume[i] / _cumulativeVolume[index] * (diff * diff);
                }

                _deviation[index] = Math.Sqrt(sd);

                Upper[index] = Result[index] + _deviation[index] * Deviation;
                Lower[index] = Result[index] - _deviation[index] * Deviation;
            }
        }
    }
}