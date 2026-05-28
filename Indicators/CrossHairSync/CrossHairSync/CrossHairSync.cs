using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections.Generic;
using System.Threading;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class CrossHairSync : Indicator
    {
        [Parameter("LineStyle", Group = "Style for Current Chart", DefaultValue = LineStyle.Solid)]
        public LineStyle LS { get; set; }
        [Parameter("Thickness", Group = "Style for Current Chart", DefaultValue = 1)]
        public int Thc { get; set; }
        [Parameter("Color", Group = "Style for Current Chart", DefaultValue = "White")]
        public string Col { get; set; }
        [Parameter("Opacity", Group = "Style for Current Chart", DefaultValue = 60)]
        public int Opc { get; set; }

        private Color Colour;

        protected override void Initialize()
        {
            Opc = (int)(255 * 0.01 * Opc);
            Colour = Color.FromArgb(Opc, Color.FromName(Col).R, Color.FromName(Col).G, Color.FromName(Col).B);
            Crosshair.AddChart(this);
            Chart.MouseMove += OnChartMouseMove;
            Chart.MouseLeave += OnChartMouseLeave;
        }

        void OnChartMouseLeave(ChartMouseEventArgs obj)
        {
            Crosshair.DeleteCrosshair();
        }

        void OnChartMouseMove(ChartMouseEventArgs obj)
        {
            Crosshair.DrawCrosshair(MarketSeries.OpenTime[(int)obj.BarIndex], obj.YValue, LS, Thc, Colour);
        }

        public override void Calculate(int index)
        {
        }
    }

    public static class Crosshair
    {
        private static List<Indicator> Indicators = new List<Indicator>();
        private static object _lock = new object();

        public static void DrawCrosshair(DateTime x, double y, LineStyle ls, int thc, Color colour)
        {
            try
            {
                lock (_lock)
                {
                    foreach (Indicator i in Indicators)
                    {
                        Thread t = new Thread(() =>
                        {
                            i.BeginInvokeOnMainThread(() => { i.Chart.DrawHorizontalLine("HorizontalCrosshairLine", y, colour, thc, ls); });
                            i.BeginInvokeOnMainThread(() => { i.Chart.DrawVerticalLine("VerticalCrosshairLine", x, colour, thc, ls); });
                        });
                        t.Start();
                    }
                }
            } catch (Exception)
            {
            }
        }

        public static void DeleteCrosshair()
        {
            try
            {
                lock (_lock)
                {
                    foreach (Indicator i in Indicators)
                    {
                        Thread t = new Thread(() =>
                        {
                            i.BeginInvokeOnMainThread(() => { i.Chart.RemoveObject("HorizontalCrosshairLine"); });
                            i.BeginInvokeOnMainThread(() => { i.Chart.RemoveObject("VerticalCrosshairLine"); });
                        });
                        t.Start();
                    }
                }
            } catch (Exception)
            {
            }
        }

        public static void AddChart(Indicator indicator)
        {
            try
            {
                lock (_lock)
                    Indicators.Add(indicator);
            } catch (Exception e)
            {
                indicator.Print(e);
            }
        }
    }
}
