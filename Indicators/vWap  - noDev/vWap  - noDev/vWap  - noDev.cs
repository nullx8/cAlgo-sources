using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using System.Net;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    //TimeZone = TimeZones.UTC
    public class IntraDayNoDev : Indicator
    {
        public string PVersion = "1.15";
        public string RemoteVersion;
        public string InfoText = "";

        [Parameter("Offset Reset time", DefaultValue = 0)]
        public int TimeOffset { get; set; }


        [Output("VWAP", PlotType = PlotType.Points, Thickness = 2, Color = Colors.Yellow)]
        public IndicatorDataSeries VWAP { get; set; }


        [Parameter("Show vWap History", DefaultValue = false)]
        public bool ShowHistoricalvWap { get; set; }

        //       [Parameter("Use OHLC", DefaultValue = false)]
        //       public bool UseOHLC { get; set; }

        [Parameter("Corner for Infos", DefaultValue = 0, MinValue = 0, MaxValue = 4)]
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
            var rv = "0";
            var client = new WebClient();
            using (var wc = new System.Net.WebClient())
                rv = wc.DownloadString("http://www.swingfish.tradev/assets/cache/app_swingfish-vwap-nodev.version.txt");

            RemoteVersion = rv.Trim();
            if ((RemoteVersion != "") & (RemoteVersion != PVersion))
            {
                corner = 2;
                Print("Version difference avaiable version:" + PVersion + "|" + RemoteVersion);
                InfoText = " vWap Update Avaiable (" + RemoteVersion + "(you have " + PVersion + ") check www.swingfish.trade ";
            }
        }

        public override void Calculate(int index)
        {
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


                    if (corner != 0)
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
                        ChartObjects.DrawText("show", "vWap " + Math.Round(VWAP[index], 5) + InfoText, corner_position);

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
