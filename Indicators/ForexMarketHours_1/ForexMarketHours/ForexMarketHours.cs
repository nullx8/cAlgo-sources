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
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace cAlgo;

[Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
public class ForexMarketHours : Indicator
{
    [Parameter("Days Back", DefaultValue = 10, MinValue = 2)]
    public int InputDaysBack { get; set; }
    
    [Parameter("Refresh Time (minutes)", DefaultValue = 5, MinValue = 1)]
    public int InputRefreshTime { get; set; }

    #region Asia

    [Parameter("Color", DefaultValue = "66008000", Group = "Asia")]
    public Color InputAsiaColor { get; set; }

    [Parameter("Visible", DefaultValue = true, Group = "Asia")]
    public bool InputAsiaVisible { get; set; }
    
    [Parameter("Filled", DefaultValue = true, Group = "Asia")]
    public bool InputAsiaFilled { get; set; }

    #endregion

    #region Europe

    [Parameter("Color", DefaultValue = "66800080", Group = "Europe")]
    public Color InputEuropeColor { get; set; }

    [Parameter("Visible", DefaultValue = true, Group = "Europe")]
    public bool InputEuropeVisible { get; set; }
    
    [Parameter("Filled", DefaultValue = true, Group = "Europe")]
    public bool InputEuropeFilled { get; set; } 

    #endregion

    #region America

    [Parameter("Color", DefaultValue = "660000FF", Group = "America")]
    public Color InputAmericaColor { get; set; }
    
    [Parameter("Visible", DefaultValue = true, Group = "America")]
    public bool InputAmericaVisible { get; set; }
    
    [Parameter("Filled", DefaultValue = true, Group = "America")]
    public bool AmericaFilled { get; set; } 

    #endregion
    
    [Parameter("Show Text", DefaultValue = true, Group = "Text")]
    public bool InputShowText { get; set; }
    
    [Parameter("Show Range", DefaultValue = true, Group = "Text")]
    public bool InputShowRange { get; set; }
    
    [Parameter("Show Average", DefaultValue = true, Group = "Text")]
    public bool InputShowAverage { get; set; }
    
    [Parameter("Show Minimum Range", DefaultValue = true, Group = "Text")]
    public bool InputShowMinimumRange { get; set; }
    
    [Parameter("Show Maximum Range", DefaultValue = true, Group = "Text")]
    public bool InputShowMaximumRange { get; set; }

    private readonly object[,] _zones = new object[5, 5];

    public const string Hide = "Sessions Off";
    public const string Show = "Sessions On";

    private readonly List<Session> _sessionList = new();
    private Color _asiaTextColor;
    private Color _europeTextColor;
    private Color _americaTextColor;
    private int _refreshTimeSeconds;
    private Button _button;
    private int _secondCounter = 0;
    public const string Asia = "Asia";
    public const string Europe = "Europe";
    public const string America = "America";

    private StackPanel _stack;
    private TextBlock _textBlock;
    
    private string ClickTheImageBelow(int seconds) => $"Click the Link Below... ({seconds})";

    protected override void Initialize()
    {
        var image = new Image
        {
            // Logo is an icon file inside project resources
            Source = Properties.Resources.my_kofi_support,
            Width = 200,
            //Height = 200,
        };

        _textBlock = new TextBlock()
        {
            Text = ClickTheImageBelow(10),
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            ForegroundColor = Chart.ColorSettings.ForegroundColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = 1,
            Padding = 1,
            Width = 198,
        };

        _button = new Button
        {
            Content = image,

            Margin = 2,
            Padding = 0,
            CornerRadius = 0           
        };

        _stack = new StackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        
//        _stack.AddChild(_textBlock);
//        _stack.AddChild(_button);

//        _button.Click += Button_Click;

//        Chart.AddControl(_stack);

        _asiaTextColor = Color.FromArgb(255, InputAsiaColor.R, InputAsiaColor.G, InputAsiaColor.B);
        _europeTextColor = Color.FromArgb(255, InputEuropeColor.R, InputEuropeColor.G, InputEuropeColor.B);
        _americaTextColor = Color.FromArgb(255, InputAmericaColor.R, InputAmericaColor.G, InputAmericaColor.B);

        var web = new HtmlWeb();
        var document = web.Load("http://forex.timezoneconverter.com/?timezone=UTC&refresh=5");
        var node = document.DocumentNode.Descendants("td").Where(d => d.GetAttributeValue("class", "").Contains("market"));
        //var node2 = document.DocumentNode.SelectNodes("//tr");
        //--

        var i = 0;
        var j = 0;

        foreach (var item in node)
        {
            //Print(item.InnerText.Trim());
            //--
            _zones[i, j++] = item.InnerText.Trim();

            if (j == 5)
            {
                i++;
                j = 0;
            }

            if (i == 5)
                break;
        }

        //---
        for (var k = 0; k < InputDaysBack; k++)
        {
            if (InputAsiaVisible)
                PlotLines(Asia, InputAsiaColor, _asiaTextColor, 4, k);
            
            if (InputEuropeVisible)
                PlotLines(Europe, InputEuropeColor, _europeTextColor, 0, k);
            
            if (InputAmericaVisible)
                PlotLines(America, InputAmericaColor, _americaTextColor, 2, k);
        }

        _refreshTimeSeconds = 60 * InputRefreshTime;
        Timer.Start(1);
        //Timer.Start(60 * InputRefreshTime);
    }

    private void Button_Click(ButtonClickEventArgs obj)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://ko-fi.com/waxycodes",
            UseShellExecute = true
        });
    }

    protected override void OnTimer()
    {
        _secondCounter++;

        _textBlock.Text = ClickTheImageBelow(10 - _secondCounter);

        if (_secondCounter == 10)
            Chart.RemoveControl(_stack);
        
        if (_secondCounter % _refreshTimeSeconds == 0)
        {
            var web = new HtmlWeb();
            var document = web.Load("http://forex.timezoneconverter.com/?timezone=UTC&refresh=5");
            var node = document.DocumentNode.Descendants("td").Where(d => d.GetAttributeValue("class", "").Contains("market"));
            //var node2 = document.DocumentNode.SelectNodes("//tr");
            //--

            var i = 0;
            var j = 0;

            foreach (var item in node)
            {
                //Print(item.InnerText.Trim());
                //--
                _zones[i, j++] = item.InnerText.Trim();

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
            for (var k = 0; k < InputDaysBack; k++)
            {
                if (InputAsiaVisible)
                    PlotLines(Asia, InputAsiaColor, _asiaTextColor, 4, k);

                if (InputEuropeVisible)
                    PlotLines(Europe, InputEuropeColor, _europeTextColor, 0, k);

                if (InputAmericaVisible)
                    PlotLines(America, InputAmericaColor, _americaTextColor, 2, k);
            }
        }

        base.OnTimer();
    }

    public void DrawStats()
    {
        foreach (var s in _sessionList.OrderBy(x => x.TimeStart))
        {
            var selectedSessions = _sessionList.Where(x => x.TimeStart <= s.TimeStart && x.Id.Contains(s.Name)).ToArray();
            var sb = new StringBuilder();
            double avg;
            double max;
            double min;
            ChartText t;
            
            if (selectedSessions.Length == 1)
            {
                avg = s.RangePips;
                max = s.RangePips;
                min = s.RangePips;    
                
                sb.AppendLine($"{s.Name}");

                if (InputShowRange)
                    sb.AppendLine($"R {s.RangePips:F1}");

                if (InputShowAverage)
                    sb.AppendLine($"Avg {avg:F1}");
                        
                if (InputShowMaximumRange)
                    sb.AppendLine($"Max {max:F1}");

                if (InputShowMinimumRange)
                    sb.AppendLine($"Min {min:F1}");

                t = Chart.DrawText(s.Name + s.TimeStart + "stats", sb.ToString(), s.MiddleTime, s.Rectangle.Y2, s.TextColor);
                t.HorizontalAlignment = HorizontalAlignment.Center;
                t.VerticalAlignment = VerticalAlignment.Bottom;
                t.IsHidden = !InputShowText;

                s.Text = t;
                continue;
            }
            
            avg = selectedSessions.Average(x => x.RangePips);
            max = selectedSessions.Max(x => x.RangePips);
            min = selectedSessions.Min(x => x.RangePips);

            sb.AppendLine($"{s.Name}");
                
            if (InputShowRange)
                sb.AppendLine($"R {s.RangePips:F1}");

            if (InputShowAverage)
                sb.AppendLine($"Avg {avg:F1}");
                        
            if (InputShowMaximumRange)
                sb.AppendLine($"Max {max:F1}");

            if (InputShowMinimumRange)
                sb.AppendLine($"Min {min:F1}");

            t = Chart.DrawText(s.Name + s.TimeStart + "stats", sb.ToString(), s.MiddleTime, s.Rectangle.Y2, s.TextColor);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Bottom;
            t.IsHidden = !InputShowText;

            s.Text = t;
            //t.IsBold = true;

            // Chart.DrawRectangle(s.Name + s.TimeStart + "avg", s.TimeStart, Open[OpenTimes.GetIndexByTime(s.TimeStart)], s.TimeEnd, Open[OpenTimes.GetIndexByTime(s.TimeStart)] + avg * Symbol.PipSize, Color.White, 1, LineStyle.Dots)
            //         .IsFilled = false;   
        }
    }

    private int[] GetIndexesFromTime(DateTime t1, DateTime t2)
    {
        int i1, i2;

        for (var i = 0;; i++)
        {
            if (OpenTimes.Last(i) > t2) 
                continue;
            
            i2 = i;
            break;
        }

        for (var i = 0;; i++)
        {
            if (OpenTimes.Last(i) > t1) 
                continue;
            
            i1 = i;
            break;
        }

        return new[] 
        {
            i1,
            i2
        };
    }

    private double[] GetRangeFromIndexes(int[] _i)
    {
        double high = 0, low = 0;

        for (var i = _i[1]; i <= _i[0]; i++)
        {
            if (High.Last(i) > high)
                high = High.Last(i);

            if (Low.Last(i) < low || low == 0)
                low = Low.Last(i);
        }

        return new[] 
        {
            high,
            low
        };
    }

    private void PlotLines(string session, Color color, Color textColor, int z, int daysBack)
    {
        for (var p = 0; p < _zones.GetLength(1); p++)
        {
            if (!_zones[z, p].ToString().Contains(session)) 
                continue;
            
            DateTime.TryParse(_zones[z, p + 2].ToString(), out var t2);
            DateTime.TryParse(_zones[z, p + 1].ToString(), out var t1);
            t1 = t1.AddDays(-daysBack);
            t2 = t2.AddDays(-daysBack);

            var range = GetRangeFromIndexes(GetIndexesFromTime(t1, t2));

            var highest = range[0];
            var lowest = range[1];
            var height = Math.Round((highest - lowest) / Symbol.PipSize, 1);

            var span = GetIndexesFromTime(t1, t2);

            if (Math.Abs(span[0] - span[1]) < 2)
                return;

            //Print("Highest is: {0}. Lowest is: {1}. ", highest, lowest);
            //Print("Range is: {0}", range.Length);

            var rectangle = Chart.DrawRectangle(session + daysBack, t1, highest, t2, lowest, color, 1, LineStyle.Solid);

            //In order to make this would I would have to make it draw the older sessions first
            // if (_sessionList.Any(x => x.Id.Contains(session)))
            // {
            //     var avgHeight = _sessionList.Where(x => x.Id.Contains(session)).Average(x => x.RangePips);
            //     
            //     Chart.DrawRectangle(session + daysBack + "avg", t1, Open[OpenTimes.GetIndexByTime(t1)], t2, Open[OpenTimes.GetIndexByTime(t1)] + avgHeight * Symbol.PipSize, Color.White, 1, LineStyle.Dots)
            //             .IsFilled = false;   
            // }

            rectangle.IsFilled = session switch
            {
                Asia => InputAsiaFilled,
                America => AmericaFilled,
                Europe => InputEuropeFilled,
                _ => rectangle.IsFilled
            };

            //ChartObjects.DrawLine(_session + " Start" + _daysback.ToString(), t1, _highest, t1, _lowest, _color, 1, LineStyle.DotsRare);
            //ChartObjects.DrawLine(_session + " End" + _daysback.ToString(), t2, _highest, t2, _lowest, _color, 1, LineStyle.DotsRare);

            // ChartText text;
            //
            // if (daysBack != 0)
            // {
            //     text = Chart.DrawText(session + "Text" + daysBack, $"{session} {height}", Close.Count - 1 - (int)(GetIndexesFromTime(t1,t2).Sum() / 2), highest + 5 * Symbol.PipSize, textColor);
            //     //ChartObjects.DrawText(_session + "Text" + _daysback.ToString(), _session, Close.Count - 1 - (int)(GetIndexesFromTime(t1, t2).Sum() / 2), _highest + 5 * Symbol.PipSize, VerticalAlignment.Center, HorizontalAlignment.Center, _color);
            // }
            // else
            // {
            //     text = Chart.DrawText(session + "Text" + daysBack, $"{session} {height}", Close.Count - 1 - (int)(GetIndexesFromTime(t1, t2).Sum() / 2) + 50, highest + 5 * Symbol.PipSize, textColor);
            //     //ChartObjects.DrawText(_session + "Text" + _daysback.ToString(), _session, Close.Count - 1 - (int)(GetIndexesFromTime(t1, t2).Sum() / 2) + 50, _highest + 5 * Symbol.PipSize, VerticalAlignment.Center, HorizontalAlignment.Center, _color);
            // }
            //
            // text.HorizontalAlignment = HorizontalAlignment.Center;
            // text.VerticalAlignment = VerticalAlignment.Top;
            
            var middleTime = Bars.OpenTimes[daysBack != 0 ? Bars.Count - 1 - GetIndexesFromTime(t1, t2).Sum() / 2 : Bars.Count - 1 - GetIndexesFromTime(t1, t2).Sum() / 2 + 50];
                
            if (_sessionList.All(x => x.Id != session + daysBack))
                _sessionList.Add(new Session($"{session}{daysBack}", session, height, rectangle, middleTime, textColor));

            //ChartObjects.DrawLine(_session + "upline" + _daysback.ToString(), t1, _highest, t2, _highest, _color, 1, LineStyle.DotsRare);
            //ChartObjects.DrawLine(_session + "downline" + _daysback.ToString(), t1, _lowest, t2, _lowest, _color, 1, LineStyle.DotsRare);

            // Print(session + " Start: {0}", t1);
            // Print(session + " Ends: {0}", t2);
            // Print(session + " TimeSpan: {0}", t2 - t1);
            // Print("---------------------------------");
            break;
        }
        
        DrawStats();
    }

    public override void Calculate(int index) { }

    public DataSeries Open => Bars.OpenPrices;
    public DataSeries High => Bars.HighPrices;
    public DataSeries Low => Bars.LowPrices;
    public DataSeries Close => Bars.ClosePrices;
    public TimeSeries OpenTimes => Bars.OpenTimes;
}