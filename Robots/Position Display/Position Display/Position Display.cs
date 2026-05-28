using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using cAlgo.API;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PositionDisplay : Robot
    {
        [Parameter("Path", DefaultValue = "%UserProfile%\\Documents\\cTrader\\Positions.txt")]
        public string FilePath { get; set; }

        private readonly string[] _ignoredSymbols = 
        {
            "USDX",
            "VIX"
        };

        private readonly Dictionary<string, string> _symbolNameMap = new Dictionary<string, string> 
        {
            {
                "GER30",
                "DAX"
            },
            {
                "FRA40",
                "CAC (EUR)"
            },
            {
                "DE30",
                "DAX"
            },
            {
                "GER40",
                "DAX (EUR)"
            },
            {
                "DE40",
                "DAX (EUR)"
            },
            {
                "XAUUSD",
                "GOLD (USD)"
            },
            {
                "XAUEUR",
                "GOLD (EUR)"
            },
            {
                "XAGUSD",
                "SILVER (USD)"
            },
            {
                "XAGEUR",
                "SILVER (EUR)"
            },
            {
                "US30",
                "DOW Jones"
            },
            {
                "UK100",
                "FTSE (GBP)"
            },
            {
                "HK50",
                "HangSeng"
            },
            {
                "AUS200",
                "ASX"
            },
            {
                "NAS100",
                "NASDAQ"
            },
            {
                "USTEC",
                "NASDAQ"
            },
            {
                "DOGEUSD",
                "DOGE (USD)"
            },
            {
                "US500",
                "SPX"
            }
        };

        private string _filePath;

        protected override void OnStart()
        {
            _filePath = Environment.ExpandEnvironmentVariables(FilePath);

            WriteToFile();

            Positions.Opened += args => WriteToFile();
            Positions.Closed += args => WriteToFile();
            Positions.Modified += args => WriteToFile();
        }

        protected override void OnTick()
        {
        }

        private void WriteToFile()
        {
            try
            {
                var positions = new Dictionary<string, double>();
                var sb = new StringBuilder();

                foreach (var position in Positions)
                {
                    if (_ignoredSymbols.Contains(position.SymbolName))
                        continue;

                    var symbol = _symbolNameMap.ContainsKey(position.SymbolName) ? _symbolNameMap[position.SymbolName] : position.SymbolName;
                    var volume = position.VolumeInUnits * (position.TradeType == TradeType.Buy ? 1 : -1);
                    double pips = position.Pips;

                    if (positions.ContainsKey(symbol))
                    {
                        positions[symbol] += volume;
                    }
                    else
                    {
                        positions.Add(symbol, volume);
                    }
                }

                foreach (var position in positions)
                {
                    if (position.Value > 0)
                    {
                        sb.AppendLine("Buy " + position.Key);
                    }
                    else if (position.Value < 0)
                    {
//                        sb.AppendLine("Sell " + position.Key + " ("+position.Value+")" );
                        sb.AppendLine("Sell " + position.Key );
                    }
                    else
                    {
                        sb.AppendLine(position.Key + " Hedged");
                    }
                }

                using (var stream = new FileStream(_filePath, FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(sb.Length == 0 ? "Waiting for Trade" : "° Active Positions\n" + sb);
                    }
                }
            } catch (Exception e)
            {
                Print(e);
            }
        }

        protected override void OnStop()
        {
            File.Delete(_filePath);
            File.Create(_filePath);
        }
    }
}
