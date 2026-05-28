using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    //TimeZone = TimeZones.UTC
    public class IntraDayStandardDeviation : Indicator
    {
        [Parameter("Offset Reset time", DefaultValue = 0)]
        public int TimeOffset { get; set; }

//        [Parameter("Deviation Level", DefaultValue = 2.8)]
//        public double DevLev { get; set; }

        [Output("Upper SD", Color = Colors.Gray, PlotType = PlotType.Points)]
        public IndicatorDataSeries SD3Pos { get; set; }

        [Output("Lower SD", Color = Colors.Gray, PlotType = PlotType.Points)]
        public IndicatorDataSeries SD3Neg { get; set; }

        [Output("VWAP", PlotType = PlotType.Points, Thickness = 2, Color = Colors.Yellow)]
        public IndicatorDataSeries VWAP { get; set; }

        [Parameter("Show Diviation Lines", DefaultValue = false)]
        public bool ShowDiviation { get; set; }

        [Parameter("Show vWap History", DefaultValue = false)]
        public bool ShowHistoricalvWap { get; set; }

        //       [Parameter("Use OHLC", DefaultValue = false)]
        //       public bool UseOHLC { get; set; }

        [Parameter("Corner for Infos", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public int corner { get; set; }

        //       private int end_bar = 0;
        private int start_bar = 0;
        private int oldCurrentDay = 0;

        public StaticPosition corner_position;
        public int CurrentDay = 0;

//      crappy cTrader 3.7 fix ?
        private MarketSeries _ms;
        public new MarketSeries MarketSeries
        {
            get
            {
                if (_ms == null)
                    _ms = base.MarketSeries;
                return _ms;
            }
        }

        protected override void Initialize()
        {
            Print("vWap - Standard deviation started v 1.14");
        }
        public override void Calculate(int index)
        {
            switch (corner)
            {
                case 1:
                    corner_position = StaticPosition.TopLeft;
                    break;
                case 2:
                    corner_position = StaticPosition.TopRight;
                    break;
                case 3:
                    corner_position = StaticPosition.BottomLeft;
                    break;
                case 4:
                    corner_position = StaticPosition.BottomRight;
                    break;
            }
            int end_bar = index;
            int CurrentDay = MarketSeries.OpenTime[end_bar].DayOfYear;
            double TotalPV = 0;
            double TotalVolume = 0;
            double highest = 0;
            double lowest = 999999;
            double close = MarketSeries.Close[index];

            if (CurrentDay == oldCurrentDay)
            {
                for (int i = start_bar; i <= end_bar; i++)
                {
                    TotalPV += MarketSeries.TickVolume[i] * ((MarketSeries.Low[i] + MarketSeries.High[i] + MarketSeries.Close[i]) / 3);
                    TotalVolume += MarketSeries.TickVolume[i];
                    VWAP[i] = TotalPV / TotalVolume;

                    if (MarketSeries.High[i] > highest)
                    {
                        highest = MarketSeries.High[i];
                    }
                    if (MarketSeries.Low[i] < lowest)
                    {
                        lowest = MarketSeries.Low[i];
                    }

                    double SD = 0;
                    for (int k = start_bar; k <= i; k++)
                    {

                        double HLC = (MarketSeries.High[k] + MarketSeries.Low[k] + MarketSeries.Close[k]) / 3;
                        double OHLC = (MarketSeries.High[k] + MarketSeries.Low[k] + MarketSeries.Open[k] + MarketSeries.Close[k]) / 4;

                        double avg = HLC;
                        double diff = avg - VWAP[i];
                        if (ShowDiviation)
                        {
                            SD += (MarketSeries.TickVolume[k] / TotalVolume) * (diff * diff);
                        }
                    }


                    if (corner != 0)
                        ChartObjects.DrawText("show", "vWap " + Math.Round(VWAP[index], 5), corner_position);

                    if (ShowDiviation)
                    {
                        SD = Math.Sqrt(SD);
//                        ChartObjects.DrawText("sda", "SD: " + (SD), 0);
//                        ChartObjects.DrawText("sdb", "SD: " + (VWAP[index]), 0);
//                        ChartObjects.DrawText("sdc", "SD: " + (close), 0);

                        double SD_Pos = VWAP[i] + SD;
                        double SD_Neg = VWAP[i] - SD;
                        double SD2Pos = SD_Pos + SD;
                        double SD2Neg = SD_Neg - SD;

                        SD3Pos[i] = SD2Pos + SD;
                        SD3Neg[i] = SD2Neg - SD;
                    }
                    if (!ShowHistoricalvWap)
                    {
                        //  VWAP[index] = sum / start_bar - i;
                        if (i < index - 15)
                        {
                            VWAP[i] = double.NaN;
                        }
                    }
                }
            }
            else
            {
                if (!ShowHistoricalvWap)
                {
                    for (int i = index - 16; i <= index; i++)
                    {
                        VWAP[i] = double.NaN;
                    }
                }
                oldCurrentDay = MarketSeries.OpenTime[end_bar].DayOfYear;
                start_bar = end_bar - TimeOffset;
            }

            return;
        }
    }
}
