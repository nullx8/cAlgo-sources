using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class PrevBarZoneFillLive_Fixed : Indicator
    {
        public enum LivePriceMode
        {
            Bid,
            Ask,
            Mid
        }

        [Parameter("Live Price", DefaultValue = LivePriceMode.Bid)]
        public LivePriceMode LivePrice { get; set; }

        [Parameter("Bull Fill", DefaultValue = "LimeGreen")]
        public Color BullFill { get; set; }

        [Parameter("Bear Fill", DefaultValue = "Tomato")]
        public Color BearFill { get; set; }

        // Neutral = "no fill"
        private readonly Color _neutral = Color.Transparent;

        public override void Calculate(int index)
        {
            // On real-time updates, cTrader typically calls Calculate() for the last bar only.
            // So when we are on the last bar, also recolor the just-closed bar to avoid stale colors.
            int last = Bars.Count - 1;

            if (index == last)
            {
                // Repaint the previous bar using its FINAL close (important!)
                PaintBar(last - 1, useLive: false);

                // Paint current bar using LIVE price
                PaintBar(last, useLive: true);
                return;
            }

            // Initial historical calculation (platform runs through all bars once)
            PaintBar(index, useLive: false);
        }

        private void PaintBar(int i, bool useLive)
        {
            if (i < 1 || i >= Bars.Count)
                return;

            double prevHigh = Bars.HighPrices[i - 1];
            double prevLow  = Bars.LowPrices[i - 1];
            double range    = prevHigh - prevLow;

            if (range <= Symbol.TickSize)
            {
                Chart.SetBarFillColor(i, _neutral);
                return;
            }

            double oneThird = range / 3.0;
            double lowTop   = prevLow + oneThird;
            double midTop   = prevLow + 2.0 * oneThird;

            double price = useLive ? GetLivePrice() : Bars.ClosePrices[i];

            if (price >= midTop) {
                Chart.SetBarFillColor(i, BullFill);
                Chart.SetBarOutlineColor(i, BullFill);
            }
            else if (price >= lowTop) {
                Chart.SetBarFillColor(i, _neutral);     // neutral = no fill
            }
            else {
                Chart.SetBarFillColor(i, BearFill);
                Chart.SetBarOutlineColor(i, BearFill);
            }
        }

        private double GetLivePrice()
        {
            switch (LivePrice)
            {
                case LivePriceMode.Ask:
                    return Symbol.Ask;
                case LivePriceMode.Mid:
                    return (Symbol.Bid + Symbol.Ask) / 2.0;
                default:
                    return Symbol.Bid;
            }
        }
    }
}
