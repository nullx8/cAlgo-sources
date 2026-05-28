using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VWAPWaveCandles : Indicator
    {
        [Parameter("Bull", Group = "Above Color", DefaultValue = "#A64CAF50")]
        public string BullUpColor { get; set; }

        [Parameter("Bear", Group = "Above Color", DefaultValue = "#A64CAF50")]
        public string BearUpColor { get; set; }

        [Parameter("Bull", Group = "Below Color", DefaultValue = "#A6FF5252")]
        public string BullDownColor { get; set; }

        [Parameter("Bear", Group = "Below Color", DefaultValue = "#A6FF5252")]
        public string BearDownColor { get; set; }

        [Parameter("Period Type", Group = "VWAP", DefaultValue = PeriodType.Daily)]
        public PeriodType Type { get; set; }

        [Parameter("Offset Pips", Group = "VWAP", DefaultValue = 2, Step = 0.1)]
        public double OffsetPips { get; set; }

        [Output("VWAP")]
        public IndicatorDataSeries VWAP { get; set; }

        [Output("Upper Band")]
        public IndicatorDataSeries UpperBand { get; set; }

        [Output("Lower Band")]
        public IndicatorDataSeries LowerBand { get; set; }

        public enum PeriodType
        {
            Daily,
            Weekly,
            Monthly
        }

        public enum TrendType
        {
            Up,
            Down
        }

        private static readonly int[] CandleWidth = 
        {
            1,
            1,
            2,
            4,
            9,
            23
        };

        private TrendType _trend;
        private Dictionary<TrendType, Color> _bullColor, _bearColor;
        private Dictionary<int, ChartTrendLine> _candles;

        private double _offset;
        private IndicatorDataSeries _cumulativeVolume;
        private IndicatorDataSeries _cumulativeTypicalVolume;

        protected override void Initialize()
        {
            _bullColor = new Dictionary<TrendType, Color> 
            {
                {
                    TrendType.Up,
                    BullUpColor
                },
                {
                    TrendType.Down,
                    BullDownColor
                }
            };
            _bearColor = new Dictionary<TrendType, Color> 
            {
                {
                    TrendType.Up,
                    BearUpColor
                },
                {
                    TrendType.Down,
                    BearDownColor
                }
            };
            _candles = new Dictionary<int, ChartTrendLine>();

            _offset = OffsetPips * Symbol.PipSize;
            _cumulativeVolume = CreateDataSeries();
            _cumulativeTypicalVolume = CreateDataSeries();

            Chart.ZoomChanged += args =>
            {
                var thickness = CandleWidth[Chart.Zoom];
                foreach (var candle in _candles.Values)
                {
                    candle.Thickness = thickness;
                }
            };
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

            VWAP[index] = _cumulativeTypicalVolume[index] / _cumulativeVolume[index];
            UpperBand[index] = VWAP[index] + _offset;
            LowerBand[index] = VWAP[index] - _offset;

            var open = MarketSeries.Open[index];
            var close = MarketSeries.Close[index];

            if (index == 0)
            {
                _trend = close > VWAP[index] ? TrendType.Up : TrendType.Down;
            }
            else
            {
                _trend = close >= UpperBand[index] ? TrendType.Up : close <= LowerBand[index] ? TrendType.Down : _trend;

                var color = close > open ? _bullColor : _bearColor;

                if (!_candles.ContainsKey(index))
                    _candles.Add(index, null);

                _candles[index] = Chart.DrawTrendLine("body" + index, index, open, index, close, color[_trend], CandleWidth[Chart.Zoom]);
            }
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
    }
}
