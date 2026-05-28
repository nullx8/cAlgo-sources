using System;
using System.Net;
using cAlgo.API;

using System.Collections.Generic;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;


namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class EnfoidApiSample : Indicator
    {
        [Parameter("Show Reset", DefaultValue = true)]
        public bool ShowReset { get; set; }

        [Parameter("Show Allowed DD", DefaultValue = true)]
        public bool ShowDD { get; set; }

        private const string Uri = "http://enfoid.com/api/a/enfoid/prop/propcheck.ctrader.php?v={0}&a={1}&s={2}";

        internal string Version = "1.71";
        internal string ServerName = "ICMarkets-Demo01";
        internal TimeSpan MaxIdleTime = TimeSpan.FromMinutes(6);

        private int _lastBarIndex;
        private DateTime _nextForcedUpdateTime;

        public double equity = 0;
        public string equityText = "";
        public string resetText = "";
        public string newsMessage = string.Empty;



        protected override void Initialize()
        {
            Timer.Start(1);
            Update();
        }

        protected override void OnTimer()
        {
            if (Time >= _nextForcedUpdateTime)
                Update();
        }

        public void Update()
        {
            using (var client = new WebClient())
            {
                Print("requesting stats");
                var request = string.Format(Uri, Version, Account.Number, ServerName);
                var response = client.DownloadString(request);
                var data = response.Split(';');
                equity = double.Parse(data[0]);
                equityText = data[1];
                resetText = data[2];
                newsMessage = data.Length > 3 ? data[3] : string.Empty;

            }

            _nextForcedUpdateTime = Time.Add(MaxIdleTime);
        }

        public override void Calculate(int index)
        {
            var outText = "";
            var ddout = Math.Round(Account.Equity - equity, 2);
            if (ShowDD)
                if (Account.Equity != Account.Balance)
                {
                    outText = outText + "EnFoid Limit: " + equity + "  [ " + ddout + " ]";
                }
                else
                {
                    outText = outText + "EnFoid Limit: " + equity + "  [" + ddout + "]";
//                    outText = outText + equityText;
                }
            if (ShowReset)
                outText = outText + " | " + resetText;

            if (Account.Equity != Account.Balance)
            {
                if (equity > 0)
                {
                    Chart.DrawStaticText("t", outText + newsMessage, VerticalAlignment.Top, HorizontalAlignment.Center, Color.YellowGreen);
                }
                else
                {
                    Chart.DrawStaticText("t", outText + newsMessage, VerticalAlignment.Top, HorizontalAlignment.Center, Color.Red);
                }
            }
            else
            {
                Chart.DrawStaticText("t", outText + newsMessage, VerticalAlignment.Top, HorizontalAlignment.Center, Color.Gray);
            }

            if (index <= _lastBarIndex)
                return;

            //           OnBarClosed(index - 1);
        }

        private void OnBarClosed(int index)
        {
            _lastBarIndex = index;

            if (Positions.Count > 0)
                Update();
        }
    }
}
