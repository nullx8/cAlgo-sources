using System;
using cAlgo.API;

namespace cAlgo;

public class Session
{
    public string Id { get; set; }
    public string Name { get; set; }
    public double RangePips { get; set; }
    public ChartRectangle Rectangle { get; set; }
    public DateTime MiddleTime { get; set; }
    public Color TextColor { get; }
    public ChartText Text { get; set; }
    public ChartTrendLine AvgUp { get; set; }
    public ChartTrendLine AvgDown { get; set; }
    public DateTime TimeStart => Rectangle.Time1;
    public DateTime TimeEnd => Rectangle.Time2;

    public Session(string id, string name, double rangePips, ChartRectangle rectangle, DateTime middleTime, Color textColor)
    {
        Id = id;
        Name = name;
        RangePips = rangePips;
        Rectangle = rectangle;
        MiddleTime = middleTime;
        TextColor = textColor;
    }
}