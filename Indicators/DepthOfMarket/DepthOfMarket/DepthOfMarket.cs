using cAlgo.API;
using System;
using System.Collections.Generic;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AutoRescale = true, AccessRights = AccessRights.None, ScalePrecision = 5)]
    public class Level2 : Indicator
    {
        MarketDepth MD;

        [Parameter("Bid color", DefaultValue = Colors.Green)]
        public string BidColor { get; set; }

        [Parameter("Ask color", DefaultValue = Colors.Red)]
        public string AskColor { get; set; }

        [Parameter("Line thickness", DefaultValue = 1.5)]
        public double lineThickness { get; set; }

        [Parameter("Line len(bars)", DefaultValue = 35)]
        public int lineLen { get; set; }

        [Parameter("Line shift(bars)", DefaultValue = 1)]
        public int indicatorShift { get; set; }

        [Parameter("Relative Value", DefaultValue = true)]
        public bool relativeValue { get; set; }

        [Parameter("Show Total orders volume", DefaultValue = true)]
        public bool showTotalOrdersVolume { get; set; }

        [Parameter("Show Delta volumes", DefaultValue = true)]
        public bool showDelta { get; set; }

        [Parameter("Show History on timeframe", DefaultValue = true)]
        public bool historyMode { get; set; }

        [Parameter("Cluster Size in Pip", DefaultValue = 10)]
        public int ClusterSize { get; set; }

        [Parameter("Show delta values in %", DefaultValue = true)]
        public bool ShowDelta { get; set; }

        private Dictionary<double, VolumeInfo> history { get; set; }

        private Colors _askColor;
        private Colors _bidColor;

        private DateTime _currentTime;

        public Level2() : base()
        {
            history = new Dictionary<double, VolumeInfo>();
        }

        protected override void Initialize()
        {
            MD = MarketData.GetMarketDepth(Symbol);
            MD.Updated += OnMdUsdUpdated;

            if (!Enum.TryParse<Colors>(BidColor, out _bidColor))
                _bidColor = Colors.Red;


            if (!Enum.TryParse<Colors>(AskColor, out _askColor))
                _askColor = Colors.Green;
        }


        void UpdateHistoryData(out double sumbid, out double sumask)
        {
            if (MarketSeries.OpenTime.LastValue != _currentTime || !historyMode)
            {
                history = new Dictionary<double, VolumeInfo>();
                _currentTime = MarketSeries.OpenTime.LastValue;
            }

            double sumBid = 0;
            double sumAsk = 0;

            foreach (var entry in MD.AskEntries)
            {
                // Arrotonda il valore del prezzo alla dimensione richiesta del cluster
                double price = Math.Round((entry.Price / ClusterSize), Symbol.Digits) * ClusterSize;

                if (history.ContainsKey(price))
                    history[price].Ask = history[price].Ask + entry.Volume;
                else
                    history.Add(price, new VolumeInfo 
                    {
                        Price = price,
                        Ask = entry.Volume
                    });

                sumAsk += entry.Volume;
            }


            foreach (var entry in MD.BidEntries)
            {
                // Arrotonda il valore del prezzo alla dimensione richiesta del cluster
                double price = Math.Round((entry.Price / ClusterSize), Symbol.Digits) * ClusterSize;

                if (history.ContainsKey(price))
                    history[price].Bid = history[price].Bid + entry.Volume;
                else
                    history.Add(price, new VolumeInfo 
                    {
                        Price = price,
                        Bid = entry.Volume
                    });
                sumBid += entry.Volume;
            }

            sumask = sumAsk;
            sumbid = sumBid;

        }

        void DrawMarkekDepth(double sum, int shift)
        {
            double valBid = 0;
            double valAsk = 0;
            double valAskBid = 0;

            foreach (var h in history)
            {
                valAsk = Math.Max(valAsk, h.Value.Ask);
                valBid = Math.Max(valBid, h.Value.Bid);
            }

            valAskBid = valAsk + valBid;

            ChartObjects.RemoveAllObjects();
            int index = MarketSeries.Close.Count - 1;

            double maxPrice = double.MinValue;
            double minPrice = double.MaxValue;
            double sumBid = 0;
            double sumAsk = 0;

            double priceMaxVolume = 0;
            double maxVolume = 0;

            foreach (var entry in history)
            {
                double _Ask = entry.Value.Ask;
                double _Bid = entry.Value.Bid;

                if (showDelta)
                {
                    double delta = _Ask - _Bid;
                    _Ask = _Bid = 0;
                    if (delta > 0)
                        _Ask = Math.Abs(delta);
                    else
                        _Bid = Math.Abs(delta);
                }

                // linea di ASK 
                {
                    double val = relativeValue ? _Ask / valAskBid : _Ask / valAsk;
                    if (val > 0)
                    {
                        int len = Math.Max(1, (int)(lineLen * val));
                        ChartObjects.DrawLine(entry.Key.ToString() + "ask", index + shift, entry.Key, index + shift + len, entry.Key, _askColor, lineThickness);
                    }
                }

                // linea di Bid
                {
                    double val = relativeValue ? _Bid / valAskBid : _Bid / valBid;
                    if (val > 0)
                    {
                        int len = Math.Max(1, (int)(lineLen * val));
                        ChartObjects.DrawLine(entry.Key.ToString() + "bid", index + shift, entry.Key, index + shift + len, entry.Key, _bidColor, lineThickness);
                    }
                }

                maxPrice = Math.Max(maxPrice, entry.Key);
                minPrice = Math.Min(minPrice, entry.Key);

                if (Math.Max(entry.Value.Bid, entry.Value.Ask) > maxVolume)
                    priceMaxVolume = entry.Key;

                maxVolume = Math.Max(maxVolume, Math.Max(entry.Value.Ask, entry.Value.Bid));
                sumAsk += entry.Value.Ask;
                sumBid += entry.Value.Bid;
            }


            ChartObjects.DrawLine("maxvolume", index + shift - 2, priceMaxVolume, index + shift + 2, priceMaxVolume, Colors.YellowGreen, 4);
            ChartObjects.DrawLine("midline", index + shift, minPrice, index + shift, maxPrice, Colors.SlateGray, 0.8);

            if (showTotalOrdersVolume)
                ChartObjects.DrawText("tot_label", (sum).ToString("N0"), index + shift, (minPrice + maxPrice) / 2, VerticalAlignment.Center, HorizontalAlignment.Center, Colors.WhiteSmoke);

            if (ShowDelta)
            {
                ChartObjects.DrawText("min/label", ((sumBid / (sumBid + sumAsk)) * 100).ToString("N2") + "%", index + shift, minPrice, VerticalAlignment.Bottom, HorizontalAlignment.Center, Colors.WhiteSmoke);
                ChartObjects.DrawText("max/label", ((sumAsk / (sumBid + sumAsk)) * 100).ToString("N2") + "%", index + shift, maxPrice, VerticalAlignment.Top, HorizontalAlignment.Center, Colors.WhiteSmoke);
            }

        }

        void OnMdUsdUpdated()
        {

            double sumBid = 0;
            double sumAsk = 0;
            UpdateHistoryData(out sumBid, out sumAsk);
            DrawMarkekDepth(sumBid + sumAsk, indicatorShift);

        }

        public override void Calculate(int index)
        {
            OnMdUsdUpdated();
        }
    }

    public class VolumeInfo
    {
        public double Price { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }

        public double Volume
        {
            get { return Ask + Bid; }
        }
    }


}
