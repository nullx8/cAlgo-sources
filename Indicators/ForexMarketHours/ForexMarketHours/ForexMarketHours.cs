//The MIT License (MIT)
//Copyright(c) <2017> <Xavier R. waxavi@outlook.com>

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using HtmlAgilityPack;
using System.Linq;

namespace cAlgo
{

    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class ForexMarketHours : Indicator
    {
        [Parameter("Days Back", DefaultValue = 5)]
        public int _DaysBack { get; set; }
        [Parameter("Refresh Time (minutes)", DefaultValue = 300)]
        public int _RefreshTime { get; set; }

        object[,] _Zones = new object[5, 5];
        object[] _EU = new object[5];
        object[] _US = new object[5];
        object[] _AS = new object[5];

        Colors _AsiaColor = Colors.Yellow;
        Colors _EuropeColor = Colors.Aquamarine;
        Colors _AmericaColor = Colors.Blue;

        private int[] GetIndexesFromTime(DateTime t1, DateTime t2)
        {
            int i1, i2;

            for (int i = 0;; i++)
            {
                if (MarketSeries.OpenTime.Last(i) <= t2)
                {
                    i2 = i;
                    break;
                }
            }

            for (int i = 0;; i++)
            {
                if (MarketSeries.OpenTime.Last(i) <= t1)
                {
                    i1 = i;
                    break;
                }
            }

            return new int[] 
            {
                i1,
                i2
            };
        }

        private double[] GetRangeFromIndexes(int[] _i)
        {
            double _high = 0, _low = 0;

            for (int i = _i[1]; i <= _i[0]; i++)
            {
                if (MarketSeries.High.Last(i) > _high)
                    _high = MarketSeries.High.Last(i);

                if (MarketSeries.Low.Last(i) < _low || _low == 0)
                    _low = MarketSeries.Low.Last(i);
            }

            return new double[] 
            {
                _high,
                _low
            };
        }

        private int GetPeriodsFromDate(DateTime _dt)
        {
            for (int i = 0;; i++)
            {
                if (MarketSeries.OpenTime.Last(i) == _dt)
                    return i;
                //Break Safety
                else if (MarketSeries.OpenTime.Last(i) < MarketSeries.OpenTime.Last(0).AddDays(-2))
                {
                    Print("Return -1");
                    return -1;
                }
            }
        }

        private void PlotLines(string _session, Colors _color, int _z, int _daysback)
        {
            for (int p = 0; p < _Zones.GetLength(1); p++)
            {
                DateTime t1, t2;
                if (_Zones[_z, p].ToString().Contains(_session))
                {
                    DateTime.TryParse(_Zones[_z, p + 2].ToString(), out t2);
                    DateTime.TryParse(_Zones[_z, p + 1].ToString(), out t1);
                    t1 = t1.AddDays(-_daysback);
                    t2 = t2.AddDays(-_daysback);

                    double[] range = GetRangeFromIndexes(GetIndexesFromTime(t1, t2));

                    double _highest = range[0];
                    double _lowest = range[1];

                    Print("Highest is: {0}. Lowest is: {1}. ", _highest, _lowest);
                    Print("Range is: {0}", range.Length);

                    ChartObjects.DrawLine(_session + " Start" + _daysback.ToString(), t1, _highest, t1, _lowest, _color, 1, LineStyle.DotsRare);
                    ChartObjects.DrawLine(_session + " End" + _daysback.ToString(), t2, _highest, t2, _lowest, _color, 1, LineStyle.DotsRare);

                    if (_daysback != 0)
                    {
                        ChartObjects.DrawText(_session + "Text" + _daysback.ToString(), _session, MarketSeries.Close.Count - 1 - (int)(GetIndexesFromTime(t1, t2).Sum() / 2), _highest + 5 * Symbol.PipSize, VerticalAlignment.Center, HorizontalAlignment.Center, _color);
                    }
                    else
                    {
                        ChartObjects.DrawText(_session + "Text" + _daysback.ToString(), _session, MarketSeries.Close.Count - 1 - (int)(GetIndexesFromTime(t1, t2).Sum() / 2) + 50, _highest + 5 * Symbol.PipSize, VerticalAlignment.Center, HorizontalAlignment.Center, _color);
                    }

                    //                   ChartObjects.DrawLine(_session + "upline" + _daysback.ToString(), t1, _highest, t2, _highest, _color, 1, LineStyle.DotsRare);
                    ChartObjects.DrawLine(_session + "downline" + _daysback.ToString(), t1, _lowest, t2, _lowest, _color, 1, LineStyle.DotsRare);

                    Print(_session + " Start: {0}", t1);
                    Print(_session + " Ends: {0}", t2);
                    Print(_session + " TimeSpan: {0}", t2 - t1);
                    Print("---------------------------------");
                    break;
                }
            }
        }

        protected override void Initialize()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load("http://forex.timezoneconverter.com/?timezone=UTC&refresh=5");
            var node = document.DocumentNode.Descendants("td").Where(d => d.GetAttributeValue("class", "").Contains("market"));
            var node2 = document.DocumentNode.SelectNodes("//tr");
            //--

            int i = 0;
            int j = 0;

            foreach (var item in node)
            {
                Console.WriteLine(item.InnerText.Trim());
                //--
                _Zones[i, j++] = item.InnerText.Trim();

                if (j == 5)
                {
                    i++;
                    j = 0;
                }

                if (i == 5)
                    break;
            }

            //---
            for (int k = 0; k < _DaysBack; k++)
            {
                PlotLines("Asia", _AsiaColor, 4, k);
                PlotLines("Europe", _EuropeColor, 0, k);
                PlotLines("America", _AmericaColor, 2, k);
            }

            Timer.Start(60 * _RefreshTime);
        }

        protected override void OnStart()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load("http://forex.timezoneconverter.com/?timezone=UTC&refresh=5");
            var node = document.DocumentNode.Descendants("td").Where(d => d.GetAttributeValue("class", "").Contains("market"));
            var node2 = document.DocumentNode.SelectNodes("//tr");
            //--

            int i = 0;
            int j = 0;

            foreach (var item in node)
            {
                Console.WriteLine(item.InnerText.Trim());
                //--
                _Zones[i, j++] = item.InnerText.Trim();

                if (j == 5)
                {
                    i++;
                    j = 0;
                }

                if (i == 5)
                    break;
            }

            //--

            //---
            for (int k = 0; k < _DaysBack; k++)
            {
                PlotLines("Asia", _AsiaColor, 4, k);
                PlotLines("Europe", _EuropeColor, 0, k);
                PlotLines("America", _AmericaColor, 2, k);
            }

            base.OnTimer();
        }

        public override void Calculate(int index)
        {

        }
    }
}
