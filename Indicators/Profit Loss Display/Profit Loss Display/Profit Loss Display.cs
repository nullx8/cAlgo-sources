using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class ProfitLossDisplay : Indicator
    {
        [Parameter("Show Symbol", DefaultValue = false)]
        public bool ShowSymbol { get; set; }

        [Parameter("Show Direction", DefaultValue = true)]
        public bool ShowDirection { get; set; }

        [Parameter("Show Account", DefaultValue = false)]
        public bool ShowAccount { get; set; }

        [Parameter("Show Numbers", DefaultValue = true)]
        public bool ShowNumbers { get; set; }

        [Parameter("Show Size", DefaultValue = true)]
        public bool ShowSize { get; set; }


        public override void Calculate(int index)
        {
            if (IsLastBar)
                DisplayPLOnChart();
        }

        private void DisplayPLOnChart()
        {
            var symbolPositions = Positions.Where(t => t.SymbolCode == Symbol.Code);
            var symbolPnL = Math.Round(100 * symbolPositions.Sum(t => t.NetProfit) / Account.Balance, 3);
            var accountPnL = Math.Round(100 * Positions.Sum(t => t.NetProfit) / Account.Balance, 3);

            var symbolTextDisplayed = Symbol.Code + " " + MarketSeries.TimeFrame;

            var dColor = Colors.LightGoldenrodYellow;
            if (symbolPnL > 0)
            {
                dColor = Colors.Lime;
            }
            else if (symbolPnL < 0)
            {
                dColor = Colors.Tomato;
            }

            if (ShowSymbol)
            {
                if (symbolPnL != 0)
                {
                    if (ShowNumbers)
                    {
                        symbolTextDisplayed = Symbol.Code + " " + MarketSeries.TimeFrame + " " + symbolPnL + "% ";
                    }
                    else
                    {
                        symbolTextDisplayed = Symbol.Code + " " + MarketSeries.TimeFrame;
                    }
                }
                //              else
                //              {
                //                  var symbolTextDisplayed = Symbol.Code;
                // only show the symbol if no position open
                //              }
                ChartObjects.DrawText(Symbol.Code + "_SymbolPnL", symbolTextDisplayed, StaticPosition.TopLeft, dColor);
            }
            else
            {
                if (ShowNumbers)
                {
                    symbolTextDisplayed = symbolPnL + " ";
                    ChartObjects.DrawText(Symbol.Code + "_SymbolPnL", symbolTextDisplayed, StaticPosition.TopLeft, dColor);
                }
            }

            if (ShowAccount)
            {
                var accountTextDisplayed = "\nTotal: " + accountPnL + "% ";
                ChartObjects.DrawText(Symbol.Code + "_AccountPnL", accountTextDisplayed, StaticPosition.TopLeft, dColor);
                ChartObjects.DrawText(Symbol.Code + "_AccountPnL", accountTextDisplayed, StaticPosition.TopLeft, dColor);
            }
            if (ShowDirection)
            {
                int PosCount = symbolPositions.Count();
//                int PosCount = symbolPositions.Count(x => x.TradeType == TradeType.Buy);
//                var PosSell = symbolPositions.Count(x => x.TradeType == TradeType.Sell);
                long VolumeBuy = symbolPositions.Where(x => x.TradeType == TradeType.Buy).Sum(x => x.Volume);
                long VolumeSell = symbolPositions.Where(x => x.TradeType == TradeType.Sell).Sum(x => x.Volume);
                long Volume = VolumeBuy - VolumeSell;
                var VolumeText = ";";
                Print(Volume);
                if ((Volume > 1000000) || (Volume < -1000000))
                {
                    VolumeText = (Volume / 1000000) + "m";
                }
                else
                {
                    VolumeText = (Volume / 1000) + "k";
                }

                //              Print(PosCount + " | " + Volume);

                var DirectionTextDisplayed = "\n";

                if (PosCount != 0)
                {
                    if (Volume > 1)
                    {
                        DirectionTextDisplayed = "\nBUYING";
                    }
                    if (Volume < 1)
                    {
                        DirectionTextDisplayed = "\nSELLING";
                    }
                    if (Volume == 0)
                    {
                        DirectionTextDisplayed = "\nHEDGED !!";
                    }

                    if (ShowNumbers)
                    {
                        if (Volume == 0)
                        {
                            DirectionTextDisplayed = DirectionTextDisplayed;
                            if (ShowSize)
                                DirectionTextDisplayed = DirectionTextDisplayed + " (" + (VolumeSell / 1000) + "k)";
                        }
                        else
                        {
                            DirectionTextDisplayed = DirectionTextDisplayed;
                            if (ShowSize)
                                DirectionTextDisplayed = DirectionTextDisplayed + " (" + VolumeText + ")";
                        }
                    }
                }
                ChartObjects.DrawText(Symbol.Code + "_AccountPnL", DirectionTextDisplayed, StaticPosition.TopLeft, dColor);
            }
        }
    }
}
