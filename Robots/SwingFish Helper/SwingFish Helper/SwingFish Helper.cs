using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

// using System.Text.Json;
// using System.Text.Json.Serialization;

/*
SwingFish Helper
Mario Hennenberger http://www.swingfish.trade/swingfish-helper

terminates ALL open Positions and delete ALL Pending Orders if Net Profit or Equity Target is reached
Auto Hedges Positions based on first position distance and/or overall Position Loss

ToDo:
    - write trades to log (beta)
    - show loss/gain per symbol
    - some times auto hedsging happens always regardless of the equity  /// this is maybe fixed already with the double call removed
    - filter Pairs (like a search box to narrow TP functions to a single Product) // replace the Glubal Switch

License:
    Creative Common "CC BY" - you are REQUIRED to mention me or swingfish.trade if you re-publish this.

Contributions:
    - Mario Hennenberger <swingfish@icloud.com> https://swongfish.trade
    - tmc. <belochjiri@hotmail.com> https://ctdn.com/users/profile/tmc.

get Updates:
    - https://ctdn.com/algos/cbots/show/1664
    - http://swingfish.trade/tools

*/

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class SwingFishHelper : Robot
    {

        public string PVersion = "2.904";
        public string RemoteVersion;

        public double CurrGain, CurrTodayGain, TodayGain, MaxGain;
        public string OpenPositions;
 
        public bool isSwingFish = true; // set to false before export for public use!!!

//        [Parameter("Current Capital", DefaultValue = 0)]
//        public double Capital { get; set; }

        [Parameter("Day Start Balance", Group = "Startup" ,DefaultValue = 0)]
        public double DayStart { get; set; }

        [Parameter("Day Start Drawdown", Group = "Startup", DefaultValue = 0)]
        public double MaxLoss { get; set; }

        [Parameter("Day Prop Limit", Group = "Startup", DefaultValue = 0)]
        public double DayPropLimit { get; set; } // cash value for Drawdown limit
        public double DayPropLimitTrade = 0;
        
        [Parameter("Day Risk", Group = "Startup", DefaultValue = 4)]
        public double DayRisk { get; set; } // Account specific risk (set daily DD)
        
        [Parameter("Hedge Active", Group = "Globals", DefaultValue = false)]
        public bool HedgeActive { get; set; }

        [Parameter("Global Hedging", Group = "Globals", DefaultValue = false)]
        public bool IsGlobal { get; set; }
//        public bool IsGlobal = true;

        [Parameter("Close instead of Hedge", Group = "Globals", DefaultValue = false)]
        public bool CloseHedge { get; set; }

        [Parameter("Hedge Max Loss (%)", Group = "Globals", DefaultValue = 0.23, MinValue = 0, Step = 0.01)]
        public double NetLossP { get; set; }

        [Parameter("Margin Call (0 = off)", Group = "Globals", DefaultValue = 0, Step = 1)]
        // max equity drawdown
        public int MarginCall { get; set; }

        [Parameter("TP Active", Group = "TP/SL Automation", DefaultValue = false)]
        public bool TPActive { get; set; }

        [Parameter("Auto Cancel after", Group = "TP/SL Automation", DefaultValue = true)]
        public bool StopAfterTP { get; set; }

        [Parameter("TP Equity", Group = "TP/SL Automation", DefaultValue = 0)]
        public double EquityTarget { get; set; }

        [Parameter("TP Profit", Group = "TP/SL Automation", DefaultValue = 0)]
        public double CashTarget { get; set; }

        public bool TimeClose = false;
//        [Parameter("TP Hour", Group = "TP/SL Automation", DefaultValue = 0)]
//        public int dCloseH { get; set; }
        public int dCloseH = 0;

//        [Parameter("TP Minute", Group = "TP/SL Automation", DefaultValue = 0)]
//        public int dCloseM { get; set; }
        public int dCloseM = 0;
        // dummy for dead feature

//        public int CloseInM = 0;
        public int CloseInS = 0;

//        [Parameter("Ingore Hedges (beta)", Group = "Settings", DefaultValue = true)]
//        public bool IgnoreHedges { get; set; }

        [Parameter("Fast Mode(100ms)", Group = "Settings (Algo)", DefaultValue = false)]
        public bool TimerMode { get; set; }

        [Parameter("Play Order Sounds", Group = "Settings (Algo)", DefaultValue = true)]
        public bool PlayOrderSounds { get; set; }

        [Parameter("Small Chart (Remove title & Status)", Group = "Settings (Algo)", DefaultValue = false)]
        public bool RemoveTitles { get; set; }


        private string documentsPath;
        private int ordersCount, positionsCount;

        [Parameter("Enable Overlay/Log", Group = "Overlay/Log", DefaultValue = false)]
        public bool OverlayActive { get; set; }
        [Parameter("add Pnl to Overlay", Group = "Overlay/Log", DefaultValue = false)]
        public bool OverlayPnl { get; set; }
        [Parameter("Overlay in Wife Mode", Group = "Overlay/Log", DefaultValue = false)]
        public bool OverlayWifemode { get; set; }
        [Parameter("use HTML File (auto-refresh)", Group = "Overlay/Log", DefaultValue = false)]
        public bool OverlayHtml { get; set; }
        [Parameter("Clear on Exit", Group = "Overlay/Log", DefaultValue = false)]
        public bool OverlayDeleteonExit { get; set; }
        [Parameter("Overlay File (no extention!)", Group = "Overlay/Log", DefaultValue = "%UserProfile%\\Documents\\cTrader\\Positions.txt")]
        public string FilePath { get; set; }

        [Parameter("Enable json Status", Group = "BETA", DefaultValue = false)]
        public bool StatsActive { get; set; }
        [Parameter("json Status location (BETA)", Group = "BETA", DefaultValue = "%UserProfile%\\Documents\\cTrader\\stats.json")]
        public string StatsFilePath { get; set; }

        private readonly string[] _ignoredSymbols = 
        {
            "USDX",
            "VIX"
        };

static string SafeDownloadString(string url, int timeoutMs = 8000, string fallback = "0,0")
{
    try
    {
        var req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "GET";
        req.Timeout = timeoutMs;
        req.ReadWriteTimeout = timeoutMs;
        req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        req.UserAgent = "SwingFishHelper/1.0";
        req.AllowAutoRedirect = true;



        using var resp = (HttpWebResponse)req.GetResponse();
        using var stream = resp.GetResponseStream();
        if (stream == null) return fallback;

        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();

    }
    catch
    {
        return fallback; // ALWAYS return a string
    }
}

static string F(double v, int dp)
{
    return Math.Round(v, dp).ToString("0." + new string('#', dp), CultureInfo.InvariantCulture);
}


void SyncOnce(string baseUrl, int accountNumber, double gainPct, double peakPct, double ddPct, double equity, string version, bool seedExtremes)
{
    var url =
        baseUrl.TrimEnd('/') + "/helper-sync.php" +
        "?account=" + Uri.EscapeDataString(accountNumber.ToString(CultureInfo.InvariantCulture)) +
        "&gain=" + Uri.EscapeDataString(F(gainPct, 4)) +
        "&peak=" + Uri.EscapeDataString(F(peakPct, 4)) +
        "&dd=" + Uri.EscapeDataString(F(ddPct, 4)) +
        "&equity=" + Uri.EscapeDataString(F(equity, 2)) +
        "&ver=" + Uri.EscapeDataString((version ?? "").Trim());

    Print("SYNC URL: " + url);
    
    var raw = (SafeDownloadString(url, 8000, "0,0") ?? "0,0").Trim().Trim('\uFEFF');

    Print("SYNC RES: [" + raw + "]");


    // Only seed on OnStart if you want (recommended).
    if (!seedExtremes) return;

    var parts = raw.Split(',');
    if (parts.Length < 2) return;

    if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var serverPeak))
        MaxGain = Math.Max(MaxGain, serverPeak);

    if (double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var serverDD))
        MaxLoss = Math.Min(MaxLoss, serverDD);
}


static string DownloadStringWithTimeout(WebClient wc, string url, int timeoutMs)
{
    // Override WebClient's request creation to inject timeouts
    // Easiest way without subclassing: use WebRequest directly.
    var req = (HttpWebRequest)WebRequest.Create(url);
    req.Method = "GET";
    req.Timeout = timeoutMs;
    req.ReadWriteTimeout = timeoutMs;
    req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

    using var resp = (HttpWebResponse)req.GetResponse();
    using var stream = resp.GetResponseStream();
    if (stream == null) return "";

    using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
    return reader.ReadToEnd();
}

    



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
                "US100",
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
        private string _filePathS;
        private readonly List<Symbol> _symbols = new List<Symbol>();
        public string LogText;
        
        string LogTextPositions;

        bool ProtectProfitMode;

        public string Flocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\cTrader\\";

        private string GetSoundPath(string soundName)
        {
            return string.Format("{0}\\cAlgo\\Sources\\Robots\\SwingFish Helper\\Sounds\\{1}", documentsPath, soundName);
        }

        private void OnPositionsOpened(PositionOpenedEventArgs obj)
        {
            if (PlayOrderSounds)
            {
                Notifications.PlaySound(GetSoundPath("OrderFilled.mp3"));
            }
        }

        private void OnPositionsClosed(PositionClosedEventArgs obj)
        {
            var position = obj.Position;

            double closingPrice = History.Where(x => x.PositionId == obj.Position.Id).Last().ClosingPrice;
            double stopLoss, takeProfit;

            if (position.TradeType == TradeType.Buy)
            {
                stopLoss = position.StopLoss != null ? (double)position.StopLoss : double.NegativeInfinity;
                takeProfit = position.TakeProfit != null ? (double)position.TakeProfit : double.PositiveInfinity;

                if (PlayOrderSounds)
                {
                    if (closingPrice >= takeProfit)
                    {
                        Notifications.PlaySound(GetSoundPath("TargetFilled.mp3"));
                    }
                    else if (closingPrice <= stopLoss)
                    {
                        Notifications.PlaySound(GetSoundPath("StopFilled.mp3"));
                    }
                    else
                    {
                        Notifications.PlaySound(GetSoundPath("OrderFilled.mp3"));
                    }
                }
            }
            else
            {
                stopLoss = position.StopLoss != null ? (double)position.StopLoss : double.PositiveInfinity;
                takeProfit = position.TakeProfit != null ? (double)position.TakeProfit : double.NegativeInfinity;

                if (PlayOrderSounds)
                {
                    if (closingPrice <= takeProfit)
                    {
                        Notifications.PlaySound(GetSoundPath("TargetFilled.mp3"));
                    }
                    else if (closingPrice >= stopLoss)
                    {
                        Notifications.PlaySound(GetSoundPath("StopFilled.mp3"));
                    }
                    else
                    {
                        Notifications.PlaySound(GetSoundPath("OrderFilled.mp3"));
                    }
                }
            }
        }
        private double AccountBalanceAtTime(DateTime dt)
        {
            var historicalTrade = History.LastOrDefault(x => x.ClosingTime < dt);
            return historicalTrade != null ? historicalTrade.Balance : Account.Balance;
        }

        protected override void OnBar()
        {
// this is anoying ! 
//            Notifications.PlaySound(GetSoundPath("OnBar.mp3"));

            if (isSwingFish) {
                UpdateSwingFishTracking();
            }
        }

        protected override void OnStart()
        {
            _filePath = Environment.ExpandEnvironmentVariables(FilePath);
            _filePathS = Environment.ExpandEnvironmentVariables(StatsFilePath);
            WriteToFile();
            Positions.Opened += args => WriteToFile();
            Positions.Closed += args => WriteToFile();
            Positions.Modified += args => WriteToFile();
        
        // dd must be negative (this is to allow positive values to be entered)
        if (MaxLoss > 0) {
            MaxLoss = MaxLoss * -1;
        }

            if (DayStart == 0)
            {
                DayStart = AccountBalanceAtTime(Time.Date);
            }

            if ((DayPropLimit == 0)&&(DayRisk!=0))
            {
                DayPropLimit = DayStart-((DayStart/100)*DayRisk);
            }

RemoteVersion = SafeDownloadString(
    "http://cloud-s3.enfoid.com/SwingFish/Downloads/CommunityCode/cAlgo/Robots/SwingFishHelper/VERSION.txt",
    timeoutMs: 8000,
    fallback: ""
);

RemoteVersion = (RemoteVersion ?? "").Trim().Trim('\uFEFF');
Print("Version: " + PVersion + "[Remote:" + RemoteVersion + "]");

        if (isSwingFish) {
            Print("SYNC PNL: ACTIVE!");
            SyncOnce("https://tradeapi.enfoid.com/api/a/swingfish/ctrader/", Account.Number, ((Account.Equity / DayStart * 100.0) - 100.0), MaxGain, MaxLoss, Account.Equity, PVersion, true);
                Batch();
       //         UpdateSwingFishTracking();
      
        }
            if ((RemoteVersion != "") & (RemoteVersion != PVersion))
            {
                Print("Version difference avaiable version:" + PVersion + "|" + RemoteVersion);
            }

//            Print(RemoteVersion);


            if (CashTarget > 0)
            {
                // we have a TP .. convert TP to equity
                EquityTarget = Account.Balance + CashTarget;
                CashTarget = 0;
            }

            // decide what mode we use (trailing or taking, time or not)
            //           if ((EquityTarget > 0) && (Math.Round(EquityTarget - Account.Equity) < 0))
            if ((EquityTarget > 0) && (EquityTarget < Account.Equity))
            {
                ProtectProfitMode = true;
                Print("SwingFish Helper: Protecting Equity: " + EquityTarget);
            }
            else
            {
                ProtectProfitMode = false;
            }

            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ordersCount = PendingOrders.Count;
            positionsCount = Positions.Count;

            Positions.Closed += OnPositionsClosed;
            Positions.Opened += OnPositionsOpened;
            Timer.Start(TimeSpan.FromMilliseconds(100));
 
            Batch();
            WriteToFile();
}

        protected override void OnTimer()
        {
            DayPropLimitTrade = Math.Round((Account.Equity-DayPropLimit)/10,2);
            
            if ((TimerMode) || (ProtectProfitMode))
            {
                Batch();
            }
            if ((PendingOrders.Count > ordersCount) & (PlayOrderSounds == true))
            {
                Notifications.PlaySound(GetSoundPath("OrderPending.mp3"));
                ordersCount = PendingOrders.Count;
            }
            else if ((PendingOrders.Count < ordersCount) & (PlayOrderSounds == true))
            {
                if (Positions.Count == positionsCount)
                {
                    Notifications.PlaySound(GetSoundPath("OrderCanceled.mp3"));
                }
                ordersCount = PendingOrders.Count;
            }
            positionsCount = Positions.Count;
        }
        protected override void OnTick()
        {
            // DayStart < starting balance 

            CurrGain = Account.Equity / Account.Balance * 100 - 100;         // current Pnl                                         was TmpEq
            CurrTodayGain = (((Account.Equity / DayStart) * 100) - 100);     // todays gain (not realized) 
            TodayGain = (((Account.Balance / DayStart) * 100) - 100);        // todays realized gain (DB: gain) 

            // based on equity
            if (CurrTodayGain > MaxGain ) { MaxGain = CurrTodayGain; }        // todays equity high (not realised) DB: peak
            if (CurrTodayGain < MaxLoss ) { MaxLoss = CurrTodayGain; }        // todays equity high (not realised) DB: drawdown     was DayStartDD
           
           Batch();
            WriteToFile();
        }

        private void OnChartMouseDown(ChartMouseEventArgs args)
        {
            if (args.ShiftKey)
                Notifications.PlaySound(GetSoundPath("OrderCanceled.mp3"));
        }

        protected override void OnStop()
        {
        if (OverlayDeleteonExit) {
            File.Delete(_filePath);
            File.Create(_filePath);
        }
            // Put your deinitialization logic here
            // will put a log here .. need to figure out how that works 
            Print("SwingFish Helper says: bye");
        }


        public void UpdateSwingFishTracking() {
                        Print(CurrTodayGain);
                        SyncOnce("https://tradeapi.enfoid.com/api/a/swingfish/ctrader/", Account.Number, CurrTodayGain, MaxGain, MaxLoss, Account.Equity, PVersion, true);
        }

        public void WriteToFile()
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
                        sb.AppendLine(position.Key + " (Hedged)");
                    }
                }

                
                // Limit orders
                var Limits = new Dictionary<string, double>();
//                var sb = new StringBuilder();

                foreach (var positionl in PendingOrders)
                {
                    if (_ignoredSymbols.Contains(positionl.SymbolName))
                        continue;

                    var symbol = _symbolNameMap.ContainsKey(positionl.SymbolName) ? _symbolNameMap[positionl.SymbolName] : positionl.SymbolName;
                    var volume = positionl.VolumeInUnits * (positionl.TradeType == TradeType.Buy ? 1 : -1);
                    //double pips = position.Pips;

                    if (Limits.ContainsKey(symbol))
                    {
                        Limits[symbol] += volume;
                    }
                    else
                    {
                        Limits.Add(symbol, volume);
                    }
                }

                foreach (var position in Limits)
                {
                    if (position.Value > 0)
                    {
                        sb.AppendLine("Buy Limit " + position.Key);
                    }
                    else if (position.Value < 0)
                    {
//                        sb.AppendLine("Sell " + position.Key + " ("+position.Value+")" );
                        sb.AppendLine("Sell Limit " + position.Key);
                    }
                    else
                    {
                        sb.AppendLine(position.Key + " Hedge Stop");
                    }
  }
          if (OverlayActive) {
              using (var stream = new FileStream(_filePath, FileMode.Create))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        if (CurrTodayGain > 0.6) { CurrTodayGain = Math.Round(CurrTodayGain,2); }
                        if(OverlayHtml) {
                            writer.Write("<html><head><meta http-equiv=\"refresh\" content=\"15\"><title>cTrader Position Overlay</title></head><body>");
                        }
                        if ((OverlayPnl) && (CurrTodayGain !=0)){
                           if ((OverlayWifemode) && (CurrGain > CurrTodayGain)) {
                                writer.Write(sb.Length == 0 ? "Waiting for Trade ["+(Math.Round(CurrGain,3) >= 0 ? "+" : "") + (Math.Round(CurrGain,3))+"% | "+Math.Abs(Math.Round(CurrGain/MaxLoss,1))+"R]" : "° Active Positions  ["+(Math.Round(CurrGain,3) >= 0 ? "+" : "") +Math.Round(CurrGain,3)+"%]\n"  + sb);
                           } else {
                                writer.Write(sb.Length == 0 ? "Waiting for Trade ["+(CurrTodayGain >= 0 ? "+" : "") + Math.Round(CurrTodayGain,3)+"% | "+Math.Abs(Math.Round(CurrTodayGain/MaxLoss,1))+"R]" : "° Active Positions  ["+(CurrTodayGain >= 0 ? "+" : "") +Math.Round(CurrTodayGain,3)+"%]\n" + sb);
                           }
                        } else {
                            writer.Write(sb.Length == 0 ? "Waiting for Trade" : "° Active Positions\n" + sb);
                        }
                        if(OverlayHtml) {
                            writer.Write("</body></html>");
                        }
                    }
                }
             }


// write json file
                    
/*
                List<data> _data = new List<data>();
                _data.Add(new data()
                {
                Id = 1,
                SSN = 2,
                Message = "A Message"
                });

                string json = JsonSerializer.Serialize(_data);
                File.WriteAllText(_filePathS, json);
*/
                using (var streamS = new FileStream(_filePathS, FileMode.Create))
                {
                    var jsonout = "{ \"Today\": {";
                        jsonout = jsonout + "\"CurrTodayGain\":"+ CurrTodayGain + ",";
                        jsonout = jsonout + "\"MaxGain\":"+ MaxGain + ",";
                        jsonout = jsonout + "\"MaxLoss\":"+ MaxLoss;
                        jsonout = jsonout + "}, \"Current\": {";
                        jsonout = jsonout + "\"CurrGain\":"+ CurrGain + ",";;
                        jsonout += "\"CurrActivity\":" +
    System.Text.Json.JsonSerializer.Serialize(
        string.Join(", ",
            sb.ToString()
              .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
              .Select(s => s.Trim())
        )
    );
                        jsonout = jsonout + "},";
                        jsonout = jsonout + "\"updated\":"+ DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        jsonout = jsonout + "}";
                    using (var writerS = new StreamWriter(streamS))
                    {
                        writerS.Write(jsonout);
                    }
                }
         
                
                
            } catch (Exception e)
            {
                Print(e);
            }
        }


    protected void Batch()
        {
            LogText = "";

            if ((MarginCall != 0) && (MarginCall > Account.Equity))
            {
                Print("Close All Orders: MarginCall" + MarginCall);
                closeAllPosition();
                closeAllOrder();
                // Kill yourself
                Stop();
            }

            if ((TPActive) && (EquityTarget != 0))
            {
                GlobalTP();
            }
            else
            {
                TPActive = false;
            }

            if (HedgeActive)
            {
                AutoHedge();
            }

            if ((dCloseM + dCloseH) > 0)
            {

                TimeClose = true;

                Int32 tNow = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                var CloseTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, dCloseH, dCloseM, 0);
                Int32 tClose = (Int32)(CloseTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                //               CloseInM = (tClose - tNow) / 60;
                CloseInS = (tClose - tNow);


//                Print("diffM: {0}", CloseInM);
//                Print("diffS: {0}", CloseInS);

                if ((DateTime.Now.Hour == dCloseH) && (DateTime.Now.Minute == dCloseM))
                {
                    Print("Close All Orders: TimeTrigger " + dCloseH + ":" + dCloseM);

                    closeAllPosition();
                    //closeAllPositionSync();
                    closeAllOrder();
                    dCloseM = 0;
                    dCloseH = 0;

                    if (StopAfterTP)
                    {
                        StopTpMode();
                    }
                }
            }


            Logging();
        }

        private void closeAllOrder()
        {
            //  close all pending orders
            foreach (PendingOrder o in PendingOrders)
                CancelPendingOrder(o);
        }

        private void closeAllPosition()
        {
            // target is reached 
            foreach (Position p in Positions)
                ClosePositionAsync(p);
        }

        private void closeAllPositionSync()
        {
            // same as CloseAllPositions() but syncs the orders
            // target is reached 
            foreach (Position p in Positions)
                ClosePosition(p);
        }

        private void closeWhenTarget(double target)
        {
            double profit = 0;

            foreach (Position p in Positions)
                profit += p.NetProfit;

            // when profit is negative, we're done
            if (profit < target)
                return;

            Print("Close All Orders: closeWhenTarget " + target);
            closeAllPosition();
            closeAllOrder();

            // go sleep
            if (StopAfterTP)
            {
                StopTpMode();
            }
        }

        private void closeWhenEquity(double target)
        {
            if (ProtectProfitMode)
            {
                // protect mode .. closing when equity lower 
                if (Account.Equity > target)
                {
                    return;
                }
                Print("Close All Orders: closeWhenEquity ProtectProfitMode" + target);
            }
            else
            {
                if (Account.Equity < target)
                {
                    //   ProtectProfitMode = true;
                    return;
                }

                Print("Close All Orders: closeWhenEquity target" + target);
            }

            closeAllPosition();
            closeAllOrder();

            // go sleep
            if (StopAfterTP)
            {
                StopTpMode();
            }
        }


        protected void GlobalTP()
        {
            closeWhenEquity(EquityTarget);
        }

        protected void StopTpMode()
        {
            Print("TP Mode off now");
            TPActive = false;
        }

        protected void AutoHedge()
        {
            // list of positions grouped by symbol code
            var groups = Positions.GroupBy(x => x.SymbolCode).ToList();

            // loop through each group of positions
            LogTextPositions = "";
            foreach (var positions in groups)
            {
                // gets symbol code
                var symbolCode = positions.First().SymbolCode;

                // skips rest of the iteration if global is disabled and the symbol is not equal to current chart symbol
                if (!IsGlobal && symbolCode != MarketSeries.SymbolCode)
                {
                    continue;
                }

                // only if a netloss is set 
                if (NetLossP != 0)
                {
                    // calculate numeric value of max loss based on percentage
                    // calculates total buy and sell volume
                    double buyPl = 0;
                    buyPl = positions.Where(x => x.TradeType == TradeType.Buy).Sum(x => x.NetProfit);
                    var buyVolume = positions.Where(x => x.TradeType == TradeType.Buy).Sum(x => x.Volume);
                    var sellVolume = positions.Where(x => x.TradeType == TradeType.Sell).Sum(x => x.Volume);

                    if (double.IsNaN(buyPl))
                    {
                        Print("SKIP HEDGING .. incorrect position value: " + buyPl);
                        continue;
                    }

                    double NetLoss = Account.Balance * (NetLossP / 100);

//                  skips rest of the iteration if net profit is above our threshold
                    if (positions.Sum(x => x.NetProfit) > -NetLoss)
                    {
                        continue;
                        var PosNet = (positions.Sum(x => x.NetProfit));
                        LogTextPositions = LogTextPositions + PosNet + " | ";
                    }

                    // skips rest of the iteration if both volumes are equal
                    if (buyVolume == sellVolume)
                    {
                        continue;
                    }

                    // if buy volume is higher than sell volume
                    if (buyVolume > sellVolume)
                    {
                        double hedgeVolume = buyVolume - sellVolume;
                        var hedgeSymbol = GetSymbol(symbolCode);
                        if (hedgeSymbol.NormalizeVolumeInUnits(hedgeVolume, RoundingMode.Up) >= (hedgeSymbol.VolumeInUnitsMax / 2))
                        {
                            hedgeVolume = hedgeSymbol.NormalizeVolumeInUnits(hedgeSymbol.VolumeInUnitsMax / 2);
                        }
                        // opens sell order equal to the difference of volumes
                        if (!CloseHedge)
                        {
                            ExecuteMarketOrder(TradeType.Sell, GetSymbol(symbolCode), hedgeVolume, "Hedge (NetLoss)");
                        }

                        if (hedgeSymbol.NormalizeVolumeInUnits(hedgeVolume, RoundingMode.Down) <= (hedgeSymbol.VolumeInUnitsMax / 2))
                        {
                            Notifications.PlaySound(GetSoundPath("TradeHedged.mp3"));
                            foreach (var position in positions)
                            {
                                if (CloseHedge)
                                {
                                    ClosePositionAsync(position);
                                }
                                else
                                {
                                    ModifyPosition(position, null, null);
                                }
                            }
                        }

                    }
                    // else sell volume is higher than buy volume
                    else
                    {
                        double hedgeVolume = sellVolume - buyVolume;
                        var hedgeSymbol = GetSymbol(symbolCode);
                        if (hedgeSymbol.NormalizeVolumeInUnits(hedgeVolume, RoundingMode.Up) >= (hedgeSymbol.VolumeInUnitsMax / 2))
                        {
                            hedgeVolume = hedgeSymbol.NormalizeVolumeInUnits(hedgeSymbol.VolumeInUnitsMax / 2);
                        }
                        // opens sell order equal to the difference of volumes
                        if (!CloseHedge)
                        {
                            ExecuteMarketOrder(TradeType.Buy, GetSymbol(symbolCode), hedgeVolume, "Hedge (NetLoss)");
                        }
                        if (hedgeSymbol.NormalizeVolumeInUnits(hedgeVolume, RoundingMode.Down) <= (hedgeSymbol.VolumeInUnitsMax / 2))
                        {
                            Notifications.PlaySound(GetSoundPath("TradeHedged.mp3"));
                            foreach (var position in positions)
                            {
                                if (CloseHedge)
                                {
                                    ClosePositionAsync(position);
                                }
                                else
                                {
                                    ModifyPosition(position, null, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method called to find symbol by its code
        /// </summary>
        private Symbol GetSymbol(string symbolCode)
        {
            // tries to find a match in our collection of symbols
            var matchingSymbol = _symbols.FirstOrDefault(x => x.Code == symbolCode);

            // returns the matching symbol if found
            if (matchingSymbol != null)
            {
                return matchingSymbol;
            }

            // else adds the symbol into the collection and returns it
            var symbol = MarketData.GetSymbol(symbolCode);
            _symbols.Add(symbol);
            return symbol;
        }

        public void Logging()
        {
            // line 1 info und status
            //           LogText = "SwingFish Helper " + RemoteVersion;
//            LogText = Symbol.Code + " | SwingFish Helper ";
            if (!RemoveTitles) { LogText = "SwingFish Helper "; }

            if (TPActive)
            {
                //      LogText = LogText + " | TP Active ";
            }
            if (HedgeActive)
            {
                //    LogText = LogText + " | Hedge Active ";
            }

            if (CurrGain != 0 ) {
                // live data
                LogText = LogText + "\nNow: " + Math.Round(CurrGain, 3) + "%";
                LogText = LogText + " | " + Math.Round(CurrTodayGain, 3) + "%";
            }
            if (DayStart > 0)
            {
                LogText = LogText + "\nDay: " + Math.Round(TodayGain, 3) + "%";

                LogText = LogText + " [ P: " + Math.Round(MaxGain, 3);

                // add Drawdown Text
                if (MaxLoss < 0) {
                    LogText = LogText + " | " + Math.Round(MaxLoss,3);
                }
                LogText = LogText + "%";
                
                if ((Account.Balance == Account.Equity)&&(Account.Equity > DayStart))
                {
                   var pnlt = Math.Abs(Math.Round(((Account.Balance / DayStart * 100 - 100) / MaxLoss),2));
                    LogText = LogText + " | " + pnlt + "R";
                }
                LogText = LogText + " ]";
                if (DayRisk != 0)
                {
                    LogText = LogText + "\rPropR: " + Math.Round(DayPropLimitTrade,0) +"  [ "+Math.Round(((DayPropLimitTrade/Account.Equity)*100),2)+"% | "+ Math.Round(DayPropLimit)+" ]";
                }
            }

    if (!RemoveTitles) {
            if ((!HedgeActive) && (!TPActive))
            {
//                LogText = LogText + " | Idle ..\n";
            }
            else
            {
                LogText = LogText + "\n";
            }

// line 2 tp
            if (TPActive)
            {
                //              LogText = LogText + "TP";
                if (EquityTarget != 0)
                {
                    if (ProtectProfitMode)
                    {
                        // protect gain mode active
                        LogText = LogText + "ProtectEquity: " + EquityTarget;
                        LogText = LogText + " | Closing: " + Math.Round(Account.Equity - EquityTarget, 2);
                    }
                    else
                    {
                        LogText = LogText + "TP at Equity: " + EquityTarget;
                        LogText = LogText + " | Dist: " + Math.Round(EquityTarget - Account.Equity, 2);
                    }
                }

                LogText = LogText + "\n";
            }
//line 3 hedge

            if (HedgeActive)
            {
                if (!CloseHedge)
                {
                    LogText = LogText + "Hedge";
                }
                else
                {
                    LogText = LogText + "Close";
                }
                if (NetLossP != 0)
                {
                    LogText = LogText + " @ " + NetLossP + "% Symbol Loss";
                }
                if (LogTextPositions != "")
                {
                    LogText = LogText + "\n" + LogTextPositions + "\n";
                }
                LogText = LogText + "\n";
            }

            if (TimeClose)
            {
                if (CloseInS > 3600)
                {
                    // show in minutes 
                    LogText = LogText + "TimeClose in: " + (CloseInS / 3600) + " Hour";
                    if ((CloseInS / 3600) > 1)
                    {
                        LogText = LogText + "s";
                    }
                }
                else if (CloseInS > 120)
                {
                    // show in minutes 
                    LogText = LogText + "TimeClose in: " + (CloseInS / 60) + " Minutes";
                }
                else
                {
                    LogText = LogText + "TimeClose in: " + CloseInS + " Secound";
                }
                LogText = LogText + "\n";
            }
            else
            {
                // dummy leere zeile das der timeclose ueberschrieben wird
                LogText = LogText + "\n";
            }
    }
//          color based on daystart
            
            var LColors = Colors.White;
            if (Account.Equity > DayStart)
            {
               // reset CurrGain to account when no positions active
               if (Account.Balance == Account.Equity) {
               // trades open?
                if (CurrGain > (MaxLoss))
                {
                   LColors = Colors.Lime;
                }
                else {
                   LColors = Colors.LightSteelBlue;
                }
               }
               else {
                LColors = Colors.Lime;
               }
               
            }
            else {
            LColors = Colors.OrangeRed;
            }

            if ((Account.MarginLevel) < 200)
            {
                LColors = Colors.PaleVioletRed;
                LogText = LogText + "\nWARNING: LOW MARGIN! " + Account.FreeMargin;
                LogText = LogText + "\n";
            }
            if ((RemoteVersion != "") & (RemoteVersion != PVersion))
            {
                LogText = LogText + "\nUpdate Avaiable! (" + RemoteVersion + ", you have " + PVersion + ") check www.swingfish.trade";
            }

            ChartObjects.DrawText("SwingFishHelperInfo", LogText, StaticPosition.TopLeft, LColors);
        }

    }
}
