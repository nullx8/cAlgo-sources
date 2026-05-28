using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AutoRescale = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VWAP : Indicator
    {
        [Parameter("0 - day / 1 = week / 2 = month", DefaultValue = 1, MinValue = 1, MaxValue = 3)]
        public int vwap { get; set; }

        [Output("Main", PlotType = PlotType.Points, Thickness = 2, Color = Colors.Yellow)]
        public IndicatorDataSeries Result { get; set; }

        private TypicalPrice typ;
        public IndicatorDataSeries tpv;

        public double CTPV, CV;

        protected override void Initialize()
        {
            CTPV = 0;
            CV = 0;
            typ = Indicators.TypicalPrice();
            tpv = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            tpv[index] = typ.Result[index] * MarketSeries.TickVolume[index];
            CTPV = 0;
            CV = 0;

            int per = 0;

            if (vwap == 1)
            {
                DayOfWeek currentDay = MarketSeries.OpenTime[index].DayOfWeek;

                while (MarketSeries.OpenTime[index - per].DayOfWeek <= currentDay && index - per > 0)
                {
                    //Print(index - per);
                    currentDay = MarketSeries.OpenTime[index - per].DayOfWeek;
                    per++;
                    CTPV += tpv[index - per];
                    CV += MarketSeries.TickVolume[index - per];
                }
            }

            else if (vwap == 2)
            {
                int month = MarketSeries.OpenTime[index].Month;
                while (MarketSeries.OpenTime[index - per].Month == month)
                {
                    per++;
                    CTPV += tpv[index - per];
                    CV += MarketSeries.TickVolume[index - per];
                }
            }

            else
            {
                int day = MarketSeries.OpenTime[index].Day;
                while (MarketSeries.OpenTime[index - per].Day == day)
                {
                    per++;
                    CTPV += tpv[index - per];
                    CV += MarketSeries.TickVolume[index - per];
                }
            }


            Result[index] = CTPV / CV;
            var vwapLabel = Chart.DrawHorizontalLine("Current mvWap Price (" + vwap + ")", Result[index], Color.Gold, 0);
            vwapLabel.IsInteractive = true;
        }
    }
}
