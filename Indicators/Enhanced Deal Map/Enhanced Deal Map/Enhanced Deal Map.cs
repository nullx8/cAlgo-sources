using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;


/// removed historical display of data (mariO)


namespace cAlgo
{
    [Author("tmc", version = 1.1)]
    [Indicator("Enhanced Deal Map", IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EnhancedDealMap : Indicator
    {
        [Parameter("Show Live", DefaultValue = true)]
        public bool ShowLive { get; set; }

        [Parameter("Max Deals To Show", DefaultValue = 20)]
        public int MaxDealsToShow { get; set; }

        [Parameter("Show Result", DefaultValue = true)]
        public bool ShowResult { get; set; }

        [Parameter("Result: Pips = 1 | GrossProfit = 2 | NetProfit = 3", DefaultValue = 1, MinValue = 1, MaxValue = 3)]
        public int Result { get; set; }

        [Parameter("Buy Color", DefaultValue = "DodgerBlue")]
        public string BuyColor { get; set; }
        [Parameter("Sell Color", DefaultValue = "DarkOrchid")]
        public string SellColor { get; set; }
        [Parameter("Win Color", DefaultValue = "LimeGreen")]
        public string WinColor { get; set; }
        [Parameter("Loss Color", DefaultValue = "IndianRed")]
        public string LossColor { get; set; }
        [Parameter("Text Color", DefaultValue = "White")]
        public string TextColor { get; set; }

        [Parameter("Line Thickness", DefaultValue = 1.2, MinValue = 0.1)]
        public double Thickness { get; set; }
        [Parameter("LineStyle: Dots = 1 | DotsRare = 2 | DotsVeryRare = 3 | Lines = 4 | LinesDots = 5 | Solid = 6", DefaultValue = 1, MinValue = 1, MaxValue = 6)]
        public int LineStyleInt { get; set; }

        public class Deal
        {
            public int PositionId { get; set; }
            public int Index { get; set; }
            public VerticalAlignment Aligment { get; set; }
        }
        private List<HistoricalTrade> historicalTrades = new List<HistoricalTrade>();
        private List<Position> liveTrades = new List<Position>();
        private List<Deal> deals = new List<Deal>();
        private Colors buyColor, sellColor, winColor, lossColor, textColor;
        private bool isColorError = true;
        private LineStyle lineStyle;

        protected override void Initialize()
        {
            historicalTrades = History.Where(x => x.SymbolCode == MarketSeries.SymbolCode && x.EntryTime >= MarketSeries.OpenTime[0]).ToList();
            historicalTrades.RemoveRange(0, Math.Max(0, historicalTrades.Count() - MaxDealsToShow));

            liveTrades = Positions.Where(x => x.SymbolCode == MarketSeries.SymbolCode && x.EntryTime >= MarketSeries.OpenTime[0]).ToList();

            if (Enum.TryParse<Colors>(BuyColor, out buyColor) && Enum.TryParse<Colors>(SellColor, out sellColor) && Enum.TryParse<Colors>(WinColor, out winColor) && Enum.TryParse<Colors>(LossColor, out lossColor) && Enum.TryParse<Colors>(TextColor, out textColor))
                isColorError = false;

            lineStyle = LineStyleInt == 1 ? LineStyle.Dots : LineStyleInt == 2 ? LineStyle.DotsRare : LineStyleInt == 3 ? LineStyle.DotsVeryRare : LineStyleInt == 4 ? LineStyle.Lines : LineStyleInt == 5 ? LineStyle.LinesDots : LineStyle.Solid;

            Positions.Opened += OnPositionsOpened;
            Positions.Closed += OnPositionsClosed;
        }

        private void OnPositionsOpened(PositionOpenedEventArgs obj)
        {
            if (obj.Position.SymbolCode == MarketSeries.SymbolCode)
            {
                liveTrades.Add(obj.Position);
            }
        }

        private void OnPositionsClosed(PositionClosedEventArgs obj)
        {
            if (obj.Position.SymbolCode == MarketSeries.SymbolCode)
            {
                liveTrades.Remove(obj.Position);
                historicalTrades.Add(History.Where(x => x.PositionId == obj.Position.Id).Last());
            }
        }

        public override void Calculate(int index)
        {
            if (isColorError)
            {
                ChartObjects.DrawText("Error", "{o,o}\n/)_)\n \" \"\nOops! Incorrect color(s).", StaticPosition.TopCenter, Colors.Red);
            }
            else if (IsLastBar)
            {
                var tradesToRemove = new List<HistoricalTrade>();
                foreach (var trade in historicalTrades)
                {
                    double result = Result == 1 ? trade.Pips : Result == 2 ? trade.GrossProfit : trade.NetProfit;
                    //  DrawTrade(trade.PositionId, trade.TradeType, trade.EntryTime, trade.EntryPrice, trade.ClosingTime, trade.ClosingPrice, result);

                    if (GetIndexByTime(trade.ClosingTime) < index)
                    {
                        tradesToRemove.Add(trade);
                    }
                }
                historicalTrades.RemoveAll(x => tradesToRemove.Contains(x));

                if (ShowLive)
                {
                    foreach (var trade in ShowLive)
                    {
                        double result = Result == 1 ? trade.Pips : Result == 2 ? trade.GrossProfit : trade.NetProfit;
                        DrawTrade(trade.Id, trade.TradeType, trade.EntryTime, trade.EntryPrice, Time, trade.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask, result);
                    }
                }
            }
        }

        private int GetIndexByTime(DateTime dateTime)
        {
            int lastIndex = MarketSeries.Open.Count - 1;
            var openTime = MarketSeries.OpenTime;
            var candleLenght = openTime[lastIndex].Subtract(openTime[lastIndex - 1]);

            for (int index = lastIndex; index >= 0; index--)
            {
                if (openTime[index] <= dateTime && dateTime < openTime[index].Add(candleLenght))
                {
                    return index;
                }
            }
            return -1;
        }

        private void DrawTrade(int positionId, TradeType tradeType, DateTime entryTime, double entryPrice, DateTime closingTime, double closingPrice, double result)
        {
            int entryIndex = GetIndexByTime(entryTime);
            int closingIndex = GetIndexByTime(closingTime);

            string resultText = string.Format("{0}{1}", result > 0 ? "+" : "", result);
            var resultColor = result >= 0 ? Colors.LimeGreen : Colors.IndianRed;

            if (tradeType == TradeType.Buy)
            {
                ChartObjects.DrawText(positionId + "ArrowHead", (char)9650 + X(2), entryIndex, MarketSeries.Low[entryIndex], VerticalAlignment.Bottom, HorizontalAlignment.Center, Colors.DodgerBlue);
                ChartObjects.DrawText(positionId + "EntryPoint", (char)9205 + X(6), entryIndex, entryPrice, VerticalAlignment.Center, HorizontalAlignment.Center, Colors.DodgerBlue);

                if (entryPrice <= closingPrice)
                {
                    if (deals.Exists(x => x.PositionId == positionId))
                    {
                        int idx = deals.FindIndex(x => x.PositionId == positionId);
                        deals[idx].Index = closingIndex;
                        deals[idx].Aligment = VerticalAlignment.Top;
                    }
                    else
                    {
                        deals.Add(new Deal 
                        {
                            PositionId = positionId,
                            Index = closingIndex,
                            Aligment = VerticalAlignment.Top
                        });
                    }

                    ChartObjects.DrawLine(positionId + "Line", entryIndex, entryPrice, closingIndex, closingPrice, winColor, Thickness, lineStyle);
                    ChartObjects.DrawText(positionId + "ClosingPoint", X(6) + (char)9204, closingIndex, closingPrice, VerticalAlignment.Center, HorizontalAlignment.Center, resultColor);

                    if (ShowResult)
                    {
                        int y = deals.Where(x => x.PositionId < positionId && x.Index == closingIndex && x.Aligment == VerticalAlignment.Top).Count() + 1;
                        for (int i = 0; i <= 16; i += 2)
                        {
                            ChartObjects.DrawText(positionId + "ResultBG" + i, X(i) + new string((char)9474, Math.Max(resultText.Length - (resultText.Contains(".") ? 1 : 0), 2)) + X(8) + new string('\n', y) + " ", closingIndex, MarketSeries.High[closingIndex], VerticalAlignment.Top, HorizontalAlignment.Center, resultColor);
                        }
                        ChartObjects.DrawText(positionId + "Result", resultText + X(3) + new string('\n', y) + " ", closingIndex, MarketSeries.High[closingIndex], VerticalAlignment.Top, HorizontalAlignment.Center, Colors.White);
                    }
                }
                else
                {
                    if (deals.Exists(x => x.PositionId == positionId))
                    {
                        int idx = deals.FindIndex(x => x.PositionId == positionId);
                        deals[idx].Index = closingIndex;
                        deals[idx].Aligment = VerticalAlignment.Bottom;
                    }
                    else
                    {
                        deals.Add(new Deal 
                        {
                            PositionId = positionId,
                            Index = closingIndex,
                            Aligment = VerticalAlignment.Bottom
                        });
                    }

                    ChartObjects.DrawLine(positionId + "Line", entryIndex, entryPrice, closingIndex, closingPrice, lossColor, Thickness, lineStyle);
                    ChartObjects.DrawText(positionId + "ClosingPoint", X(6) + (char)9204, closingIndex, closingPrice, VerticalAlignment.Center, HorizontalAlignment.Center, resultColor);

                    if (ShowResult)
                    {
                        int y = deals.Where(x => x.PositionId < positionId && x.Index == closingIndex && x.Aligment == VerticalAlignment.Bottom).Count() + 1;
                        for (int i = 0; i <= 16; i += 2)
                        {
                            ChartObjects.DrawText(positionId + "ResultBG" + i, new string('\n', y) + X(i) + new string((char)9474, Math.Max(resultText.Length - (resultText.Contains(".") ? 1 : 0), 2)) + X(8), closingIndex, MarketSeries.Low[closingIndex], VerticalAlignment.Bottom, HorizontalAlignment.Center, resultColor);
                        }
                        ChartObjects.DrawText(positionId + "Result", new string('\n', y) + resultText + X(3), closingIndex, MarketSeries.Low[closingIndex], VerticalAlignment.Bottom, HorizontalAlignment.Center, Colors.White);
                    }
                }
            }
            else
            {
                ChartObjects.DrawText(positionId + "ArrowHead", (char)9660 + X(2), entryIndex, MarketSeries.High[entryIndex], VerticalAlignment.Top, HorizontalAlignment.Center, Colors.DarkOrchid);
                ChartObjects.DrawText(positionId + "EntryPoint", (char)9205 + X(6), entryIndex, entryPrice, VerticalAlignment.Center, HorizontalAlignment.Center, Colors.DarkOrchid);


                if (entryPrice >= closingPrice)
                {
                    if (deals.Exists(x => x.PositionId == positionId))
                    {
                        int idx = deals.FindIndex(x => x.PositionId == positionId);
                        deals[idx].Index = closingIndex;
                        deals[idx].Aligment = VerticalAlignment.Bottom;
                    }
                    else
                    {
                        deals.Add(new Deal 
                        {
                            PositionId = positionId,
                            Index = closingIndex,
                            Aligment = VerticalAlignment.Bottom
                        });
                    }

                    ChartObjects.DrawLine(positionId + "Line", entryIndex, entryPrice, closingIndex, closingPrice, winColor, Thickness, lineStyle);
                    ChartObjects.DrawText(positionId + "ClosingPoint", X(6) + (char)9204, closingIndex, closingPrice, VerticalAlignment.Center, HorizontalAlignment.Center, resultColor);

                    if (ShowResult)
                    {
                        int y = deals.Where(x => x.PositionId < positionId && x.Index == closingIndex && x.Aligment == VerticalAlignment.Bottom).Count() + 1;
                        for (int i = 0; i <= 16; i += 2)
                        {
                            ChartObjects.DrawText(positionId + "ResultBG" + i, new string('\n', y) + X(i) + new string((char)9474, Math.Max(resultText.Length - (resultText.Contains(".") ? 1 : 0), 2)) + X(8), closingIndex, MarketSeries.Low[closingIndex], VerticalAlignment.Bottom, HorizontalAlignment.Center, resultColor);
                        }
                        ChartObjects.DrawText(positionId + "Result", new string('\n', y) + resultText + X(3), closingIndex, MarketSeries.Low[closingIndex], VerticalAlignment.Bottom, HorizontalAlignment.Center, Colors.White);
                    }
                }
                else
                {
                    if (deals.Exists(x => x.PositionId == positionId))
                    {
                        int idx = deals.FindIndex(x => x.PositionId == positionId);
                        deals[idx].Index = closingIndex;
                        deals[idx].Aligment = VerticalAlignment.Top;
                    }
                    else
                    {
                        deals.Add(new Deal 
                        {
                            PositionId = positionId,
                            Index = closingIndex,
                            Aligment = VerticalAlignment.Top
                        });
                    }

                    ChartObjects.DrawLine(positionId + "Line", entryIndex, entryPrice, closingIndex, closingPrice, lossColor, Thickness, lineStyle);
                    ChartObjects.DrawText(positionId + "ClosingPoint", X(6) + (char)9204, closingIndex, closingPrice, VerticalAlignment.Center, HorizontalAlignment.Center, resultColor);

                    if (ShowResult)
                    {
                        int y = deals.Where(x => x.PositionId < positionId && x.Index == closingIndex && x.Aligment == VerticalAlignment.Top).Count() + 1;
                        for (int i = 0; i <= 16; i += 2)
                        {
                            ChartObjects.DrawText(positionId + "ResultBG" + i, X(i) + new string((char)9474, Math.Max(resultText.Length - (resultText.Contains(".") ? 1 : 0), 2)) + X(8) + new string('\n', y) + " ", closingIndex, MarketSeries.High[closingIndex], VerticalAlignment.Top, HorizontalAlignment.Center, resultColor);
                        }
                        ChartObjects.DrawText(positionId + "Result", resultText + X(3) + new string('\n', y) + " ", closingIndex, MarketSeries.High[closingIndex], VerticalAlignment.Top, HorizontalAlignment.Center, Colors.White);
                    }
                }
            }
        }

        private string X(int length)
        {
            return new string((char)8202, length);
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class Author : System.Attribute
    {
        public string name;
        public double version;

        public Author(string name)
        {
            this.name = name;
            version = 1.0;
        }
    }
}
