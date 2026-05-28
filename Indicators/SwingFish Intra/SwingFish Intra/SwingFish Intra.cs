using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Net;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.Internet)]
    public class SwingFishIntra : Indicator
    {
        public string PVersion = "2.47";
        public string RemoteVersion;


        [Parameter("Show Symbol", DefaultValue = true)]
        public bool ShowSymbol { get; set; }

        [Parameter("Show Direction", DefaultValue = true)]
        public bool ShowDirection { get; set; }

        [Parameter("Show Values", DefaultValue = true)]
        public bool ShowVolume { get; set; }

        [Parameter("% Line", DefaultValue = 0, Step = 0.01)]
        public double Percent { get; set; }

        [Parameter("Show vWap", DefaultValue = true)]
        public bool ShowVwap { get; set; }

        [Parameter("Show vWap History", DefaultValue = false)]
        public bool ShowHistoricalvWap { get; set; }

        [Output("ATR High", LineStyle = LineStyle.Dots, Thickness = 1, Color = Colors.LightSlateGray, PlotType = PlotType.Points)]
        public IndicatorDataSeries atrH { get; set; }
        [Output("ATR Low", LineStyle = LineStyle.Dots, Thickness = 1, Color = Colors.LightSlateGray, PlotType = PlotType.Points)]
        public IndicatorDataSeries atrL { get; set; }

        [Parameter("vWap Reset Offset", DefaultValue = 0)]
        public int TimeOffset { get; set; }

        [Parameter("vWap Strength level", DefaultValue = 0.05, Step = 0.001)]
        public double vWapStrength { get; set; }

        [Output("VWAP", LineStyle = LineStyle.Dots, Thickness = 2, Color = Colors.Yellow, PlotType = PlotType.Points)]
        public IndicatorDataSeries VWAP { get; set; }


        private class PriceLevel
        {
            public ChartHorizontalLine LineObject { get; set; }
            public ChartText TextObject { get; set; }
        }

        private Dictionary<TradeType, List<Position>> _positions;
        private Dictionary<TradeType, PriceLevel> _priceLevel;

        public string VolumeDownColor = "Red";
        public string VolumeUpColor = "Green";



//        [Parameter("Coloring based on real volume?", DefaultValue = "true")]
//        public bool UseRealVolume { get; set; }
        public bool UseRealVolume = true;

        private const String arrowUp = "▲";
        private const String arrowDown = "▼";
        private const String arrowFlat = "►";

//       private int end_bar = 0;
        private int start_bar = 0;
        private int oldCurrentDay = 0;

        public StaticPosition InfoLocation;

        private AverageTrueRange atr;
        private RelativeStrengthIndex rsi;
        public double[,] RealtimeVwap = new double[700, 2];
        public MarketSeries M1;
        public int BarCount;
        public int TickVolumeHigher = 0;

        public double vWapA = 0;
        public double vWapB = 0;
        public double vWapDiff = 0;

        public string symbolTextDisplayed;

        /// format numbers
        public string HumanNumber(double number)
        {
            if (number >= 1000000)
                return (number / 1000000).ToString() + "m";
            else if (number >= 1000)
                return (number / 1000).ToString() + "k";

            return number.ToString();
        }
        /// Default Sizing Function
        /// GetRiskSize(_pips, _risk)
        private double GetRiskSize(double pips, double risk = 1)
        {
            return Math.Min(Symbol.NormalizeVolumeInUnits((Account.Balance * risk / 100) / (pips * Symbol.PipValue), RoundingMode.Down), Symbol.VolumeInUnitsMax);
        }

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

            atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            rsi = Indicators.RelativeStrengthIndex(MarketSeries.Close, 14);
            var client = new WebClient();

            using (var wc = new System.Net.WebClient())
 //               RemoteVersion = wc.DownloadString("http://128.199.132.87/assets/cache/app_swingfish-intra.version.txt");

            if (Percent != 0)
            {
                Percent /= 100;

                var symbolPositions = Positions.Where(x => x.SymbolCode == Symbol.Code).ToList();
                _positions = new Dictionary<TradeType, List<Position>> 
                {
                    {
                        TradeType.Buy,
                        new List<Position>(symbolPositions.Where(x => x.TradeType == TradeType.Buy))
                    },
                    {
                        TradeType.Sell,
                        new List<Position>(symbolPositions.Where(x => x.TradeType == TradeType.Sell))
                    }
                };
                _priceLevel = new Dictionary<TradeType, PriceLevel> 
                {
                    {
                        TradeType.Buy,
                        null
                    },
                    {
                        TradeType.Sell,
                        null
                    }
                };

                CalculatePnlPrice(TradeType.Buy);
                CalculatePnlPrice(TradeType.Sell);

                Positions.Opened += args =>
                {
                    if (args.Position.SymbolCode == Symbol.Code)
                    {
                        _positions[args.Position.TradeType].Add(args.Position);
                        CalculatePnlPrice(args.Position.TradeType);
                    }
                };

                Positions.Closed += args =>
                {
                    if (_positions[args.Position.TradeType].Contains(args.Position))
                    {
                        _positions[args.Position.TradeType].Remove(args.Position);
                        CalculatePnlPrice(args.Position.TradeType);
                    }
                };

                Positions.Modified += args =>
                {
                    if (_positions[TradeType.Buy].Contains(args.Position) || _positions[TradeType.Sell].Contains(args.Position))
                    {
                        if (_positions[TradeType.Buy].Contains(args.Position) && args.Position.TradeType == TradeType.Sell)
                        {
                            _positions[TradeType.Buy].Remove(args.Position);
                            _positions[TradeType.Sell].Add(args.Position);
                        }
                        else if (_positions[TradeType.Sell].Contains(args.Position) && args.Position.TradeType == TradeType.Buy)
                        {
                            _positions[TradeType.Sell].Remove(args.Position);
                            _positions[TradeType.Buy].Add(args.Position);
                        }

                        CalculatePnlPrice(TradeType.Buy);
                        CalculatePnlPrice(TradeType.Sell);
                    }
                };

                Chart.ScrollChanged += args =>
                {
                    foreach (var priceLevel in _priceLevel.Values)
                    {
                        if (priceLevel != null)
                        {
                            priceLevel.TextObject.Time = MarketSeries.OpenTime[Chart.LastVisibleBarIndex];
                        }
                    }
                };
            }
        }


        public override void Calculate(int index)
        {
            if (IsLastBar)
            {
                DisplayPLOnChart();

                if (Math.Round(rsi.Result.Last(0)) > 0 && Math.Round(rsi.Result.Last(0)) <= 30)
                {
                    ChartObjects.DrawText("rsi_label", " " + arrowDown, index + 3, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Lime);
                }
                else if (Math.Round(rsi.Result.Last(0)) >= 30 && Math.Round(rsi.Result.Last(0)) <= 50)
                {
                    ChartObjects.DrawText("rsi_label", " " + arrowDown, index + 3, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Crimson);
                }

                else if (Math.Round(rsi.Result.Last(0)) >= 50 && Math.Round(rsi.Result.Last(0)) <= 70)
                {
                    ChartObjects.DrawText("rsi_label", " " + arrowUp, index + 3, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Lime);
                }

                else if (Math.Round(rsi.Result.Last(0)) >= 70 && Math.Round(rsi.Result.Last(0)) <= 100)
                {
                    ChartObjects.DrawText("rsi_label", " " + arrowUp, index + 3, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Crimson);
                }



                var cColor = "";
                if (Symbol.Bid > VWAP[index] && vWapDiff < vWapStrength)
                {
                    TickVolumeHigher = 3;
//                    Chart.ColorSettings.BackgroundColor = Color.Aqua;
                    // price higher but difference too small show down 
                }
                else if (Symbol.Bid > VWAP[index] && vWapDiff > vWapStrength)
                {
                    TickVolumeHigher = 1;
//                    Chart.ColorSettings.BackgroundColor = Color.Azure;
                    // price higher difference big show up 
                }
                else if (Symbol.Bid < VWAP[index] && vWapDiff < vWapStrength)
                {
                    TickVolumeHigher = 3;
//                    Chart.ColorSettings.BackgroundColor = Color.BlueViolet;
                    // price below but difference too small UP 
                }
                else if (Symbol.Bid < VWAP[index] && vWapDiff > vWapStrength)
                {
                    TickVolumeHigher = 2;
//                    Chart.ColorSettings.BackgroundColor = Color.BlanchedAlmond;
                    // price below difference big show DOWN 
                }
                else
                {
                    TickVolumeHigher = 3;
//                    Chart.ColorSettings.BackgroundColor = Color.Black;
                }

//                int red = (int)Math.Max(-(MAxInt * (MarketSeries.Close[index] - bk.Hprc[index]) / (bk.Hsqh3[index] - bk.Hprc[index])), 0);
//                int green = (int)Math.Max((MAxInt * (MarketSeries.Close[index] - bk.Hprc[index]) / (bk.Hsqh3[index] - bk.Hprc[index])), 0);
//                Chart.ColorSettings.BackgroundColor = Color.Color.FromArgb(Opc, red, green, 0);


                if (((vWapDiff * 2.8) > 1) && (Symbol.Bid > VWAP[index]))
                {
                    ChartObjects.DrawText("tot_label", arrowUp, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Lime);
                }
                else if (((vWapDiff * 2.8) > 1) && (Symbol.Bid < VWAP[index]))
                {
                    ChartObjects.DrawText("tot_label", arrowDown, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Crimson);
                }
                else
                {
                    ChartObjects.DrawText("tot_label", arrowFlat, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Aquamarine);
                }

                                /*
                if (TickVolumeHigher == 1)
                {
                    ChartObjects.DrawText("tot_label", arrowUp, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Lime);
                }
                else if (TickVolumeHigher == 2)
                {
                    ChartObjects.DrawText("tot_label", arrowDown, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Crimson);
                }
                else
                {
                    ChartObjects.DrawText("tot_label", arrowFlat, index + 2, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Aquamarine);
                }
*/

if (rsi.Result.LastValue < 50)
                {
                    InfoLocation = StaticPosition.TopRight;
                }
                else
                {
                    InfoLocation = StaticPosition.BottomRight;
                }


                // vWap distance pips 
                ChartObjects.DrawText("vwapdst", " " + Math.Round((Math.Abs(vWapA - Symbol.Bid) / Symbol.PipSize), 1), index + 5, VWAP[index], VerticalAlignment.Center, HorizontalAlignment.Center, Colors.Gray);

                // Breakeven Line
                double _avrgB = 0;
                double _avrgS = 0;
                double _lotsB = 0;
                double _lotsS = 0;


                double _avrgA = 0;
                double _lotsA = 0;

                double _tpB = 0;
                double _slB = 0;
                double _tpS = 0;
                double _slS = 0;

                foreach (var position1 in Account.Positions)
                {

                    if (Symbol.Code == position1.SymbolCode)
                    {

                        if (position1.TradeType == TradeType.Sell)
                        {
                            _avrgB = _avrgB + (position1.Volume * position1.EntryPrice);
                            _lotsB = _lotsB + position1.Volume;
                        }

                        if (position1.TradeType == TradeType.Buy)
                        {
                            _avrgS = _avrgS + (position1.Volume * position1.EntryPrice);
                            _lotsS = _lotsS + position1.Volume;
                        }


                    }

                }

                _avrgB = Math.Round(_avrgB / _lotsB, Symbol.Digits);
                _avrgS = Math.Round(_avrgS / _lotsS, Symbol.Digits);

                if (_avrgS > 0)
                {
                    ChartObjects.DrawLine("brlineS", index - 2, _avrgS, index + 5, _avrgS, Colors.Lime);
                    var brlineSL = Chart.DrawHorizontalLine("Average Buy Price", _avrgS, Color.Lime, 0);
                    brlineSL.IsInteractive = true;

                }

                else
                {
                    ChartObjects.RemoveObject("brlineS");
                    ChartObjects.RemoveObject("Average Buy Price");
                }

                if (_avrgB > 0)
                {
                    ChartObjects.DrawLine("brlineL", index - 2, _avrgB, index + 5, _avrgB, Colors.Crimson);
                    var brlineLL = Chart.DrawHorizontalLine("Average Sell Price", _avrgB, Color.Crimson, 0);
                    brlineLL.IsInteractive = true;
                }
                else
                {
                    ChartObjects.RemoveObject("brlineL");
                    ChartObjects.RemoveObject("Average Sell Price");
                }
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
//                    vWapA = Math.Round(VWAP[index], 5);
//                    vWapB = Math.Round(VWAP[index - 1], 5);
                    vWapA = VWAP[index];
                    vWapB = ((VWAP[index] + VWAP[index - 1] + VWAP[index - 1] + VWAP[index - 1] + VWAP[index - 2]) / 5);
                    vWapDiff = Math.Abs(((vWapA - vWapB) / vWapA) * 100000);

                    if (MarketSeries.High[i] > highest)
                    {
                        highest = MarketSeries.High[i];
                    }
                    if (MarketSeries.Low[i] < lowest)
                    {
                        lowest = MarketSeries.Low[i];
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
            var vwapLabel = Chart.DrawHorizontalLine("Current vWap Price", VWAP[index], Color.Yellow, 0);
            vwapLabel.IsInteractive = true;

            return;
        }

        private void CalculatePnlPrice(TradeType tradeType)
        {
            if (_positions[tradeType].Count == 0)
            {
                if (_priceLevel[tradeType] != null)
                {
                    Chart.RemoveObject(_priceLevel[tradeType].LineObject.Name);
                    Chart.RemoveObject(_priceLevel[tradeType].TextObject.Name);

                    _priceLevel[tradeType] = null;
                }

                return;
            }

            double volume = 0, entryPrice = 0, commissions = 0, swaps = 0;
            foreach (var position in _positions[tradeType])
            {
                volume += position.VolumeInUnits;
                entryPrice += position.EntryPrice * position.VolumeInUnits;
                commissions += position.Commissions;
                swaps += position.Swap;
            }

            var pips = (Account.Balance * Percent + commissions + swaps) / (volume * Symbol.PipValue);
            var price = entryPrice / volume + pips * Symbol.PipSize * (tradeType == TradeType.Buy ? 1 : -1);
            //           var color = tradeType == TradeType.Buy ? Chart.ColorSettings.BuyColor : Chart.ColorSettings.SellColor;
            var color = Color.SlateGray;
            _priceLevel[tradeType] = new PriceLevel 
            {
                LineObject = Chart.DrawHorizontalLine("line" + tradeType, price, color, 1, LineStyle.Dots),
//                TextObject = Chart.DrawText("text" + tradeType, string.Format("{0}: {1:P} @{2}", tradeType, Percent, Math.Round(price, Symbol.Digits)), Chart.LastVisibleBarIndex, price, color)
//                TextObject = Chart.DrawText("text" + tradeType, string.Format("{0}: {1:P}", tradeType, Percent), Chart.LastVisibleBarIndex, price, color)
                TextObject = Chart.DrawText("text" + tradeType, string.Format("{0}", tradeType), Chart.LastVisibleBarIndex, price, color)
            };

            _priceLevel[tradeType].TextObject.HorizontalAlignment = HorizontalAlignment.Left;
        }
        public void DisplayPLOnChart()
        {


            symbolTextDisplayed = " ";
            var symbolPositions = Positions.Where(t => t.SymbolCode == Symbol.Code);
            var symbolPnL = Math.Round(100 * symbolPositions.Sum(t => t.NetProfit) / Account.Balance, 3);
            var accountPnL = Math.Round(100 * Positions.Sum(t => t.NetProfit) / Account.Balance, 3);

            if (ShowSymbol)
            {
                symbolTextDisplayed = symbolTextDisplayed + Symbol.Code + " " + MarketSeries.TimeFrame;

                if (symbolPnL == 0)
                {
                    if (Symbol.Bid > vWapA)
                    {
//                        symbolTextDisplayed = symbolTextDisplayed + " (" + Math.Round((Math.Abs(vWapA - Symbol.Bid) / Symbol.PipSize), 1) + " | " + HumanNumber(GetRiskSize(((Math.Abs(vWapA - Symbol.Ask) / Symbol.PipSize) + 1), Math.Abs(Percent * 100))) + ")";
                        symbolTextDisplayed = symbolTextDisplayed + " (" + HumanNumber(GetRiskSize(((Math.Abs(vWapA - Symbol.Ask) / Symbol.PipSize) + 1), Math.Abs(Percent * 100))) + ")";
                    }
                    else if (Symbol.Ask < vWapA)
                    {
//                        symbolTextDisplayed = symbolTextDisplayed + " (" + Math.Round((Math.Abs(vWapA - Symbol.Bid) / Symbol.PipSize), 1) + " | " + HumanNumber(GetRiskSize(((Math.Abs(vWapA - Symbol.Bid) / Symbol.PipSize) + 1), Math.Abs(Percent * 100))) + ")";
                        symbolTextDisplayed = symbolTextDisplayed + " (" + HumanNumber(GetRiskSize(((Math.Abs(vWapA - Symbol.Bid) / Symbol.PipSize) + 1), Math.Abs(Percent * 100))) + ")";
                    }
                    else
                    {
                        symbolTextDisplayed = symbolTextDisplayed + " (?)";
                    }
                }
            }

            if (symbolPnL != 0 && ShowVolume)
            {
                symbolTextDisplayed = symbolTextDisplayed + " " + symbolPnL + "% ";
            }

            if (ShowDirection)
            {
                int PosCount = symbolPositions.Count();
                long VolumeBuy = Positions.Where(x => x.TradeType == TradeType.Buy && x.SymbolCode == Symbol.Code).Sum(x => x.Volume);
                long VolumeSell = Positions.Where(x => x.TradeType == TradeType.Sell && x.SymbolCode == Symbol.Code).Sum(x => -x.Volume);

                double VolumeSellC = Positions.Where(x => x.TradeType == TradeType.Sell && x.SymbolCode == Symbol.Code).Sum(x => -x.Volume);
                long Volume = VolumeBuy + VolumeSell;
                double VolumeC = VolumeBuy + VolumeSell;
                var VolumeText = ";";

                if (Volume == 0)
                {
                    VolumeC = VolumeSellC;
                }
                else
                {
                    VolumeC = Volume;
                }


                if ((VolumeC > 999999) || (VolumeC < -999999))
                {
                    VolumeText = Math.Round((VolumeC / 1000000), 1) + "m";
                }
                else if ((VolumeC > 999) || (VolumeC < -999))
                {
                    VolumeText = Math.Round((VolumeC / 1000), 1) + "k";
                }
                else
                {
                    VolumeText = (VolumeC) + "";
                }

                var DirectionTextDisplayed = "\n";

                if (PosCount != 0)
                {
                    if (Volume > 0)
                    {
                        DirectionTextDisplayed = "BUYING";
                    }
                    if (Volume < 0)
                    {
                        DirectionTextDisplayed = "SELLING";
                    }
                    if (Volume == 0)
                    {
                        DirectionTextDisplayed = "HEDGED !!";
                    }

                    if (ShowVolume)
                    {
                        DirectionTextDisplayed = DirectionTextDisplayed + " (" + VolumeText + ")";
                    }
                    symbolTextDisplayed = symbolTextDisplayed + " | " + DirectionTextDisplayed;
                }
            }
//                ChartObjects.DrawText(Symbol.Code + "_AccountPnL", DirectionTextDisplayed, InfoLocation, Volume >= 0 ? Colors.White : Colors.LightBlue);

            if (PVersion != RemoteVersion)
            {
                symbolTextDisplayed = symbolTextDisplayed + " Update Avaiable (" + RemoteVersion + ") check www.swingfish.trade ";
            }

            //           symbolTextDisplayed = symbolTextDisplayed + " | " + Math.Round(vWapDiff, 4);
            if (symbolPnL == 0)
            {
                ChartObjects.DrawText(Symbol.Code + "_SymbolPnL", symbolTextDisplayed, InfoLocation, Colors.White);
            }
            else
            {
                ChartObjects.DrawText(Symbol.Code + "_SymbolPnL", symbolTextDisplayed, InfoLocation, symbolPnL > 0 ? Colors.Lime : Colors.Tomato);
            }
        }
    }
}
