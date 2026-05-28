using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class vWapRider : Robot
    {
        private IntraDayNoDev vwap;

        [Parameter(DefaultValue = 1.5)]
        public double vWapoffset { get; set; }

        [Parameter(DefaultValue = 2)]
        public double SL { get; set; }

        [Parameter(DefaultValue = 3)]
        public double Trail { get; set; }

        private const string label = "vWap-Rider";

        protected override void OnStart()
        {
            vwap = Indicators.GetIndicator<IntraDayNoDev>(0, true, 0);
        }

        protected override void OnBar()
        {
            if (vwap.VWAP.LastValue > Symbol.Bid)
            {
                if (PendingOrders.Count(item => item.OrderType == PendingOrderType.Limit && item.TradeType == TradeType.Sell) == 0 && Positions.Find(label, Symbol, TradeType.Sell) == null)
                {
                    if (Symbol.Spread < (vWapoffset))
                        PlaceLimitOrder(TradeType.Sell, Symbol, 1000, vwap.VWAP.LastValue - vWapoffset * Symbol.PipSize, label, SL, 0);
                }
                else
                {
                    foreach (var order in PendingOrders)
                    {
                        if (order.Label == label)
                        {
                            ModifyPendingOrder(order, vwap.VWAP.LastValue - vWapoffset * Symbol.PipSize);
                        }
                    }

                }
            }
            else
            {
                if (PendingOrders.Count(item => item.OrderType == PendingOrderType.Limit && item.TradeType == TradeType.Buy) == 0 && Positions.Find(label, Symbol, TradeType.Buy) == null)
                {
                    if (Symbol.Spread < (vWapoffset))
                        PlaceLimitOrder(TradeType.Buy, Symbol, 1000, vwap.VWAP.LastValue + vWapoffset * Symbol.PipSize, label, SL, 0);
                }
                else
                {
                    foreach (var order in PendingOrders)
                    {
                        if (order.Label == label)
                        {
                            ModifyPendingOrder(order, vwap.VWAP.LastValue + vWapoffset * Symbol.PipSize);
                        }
                    }
                }
            }
        }
        protected override void OnTick()
        {
            //           Print(vwap.VWAP.LastValue);

            var position = Positions.Find(label);
            if (position == null)
                return;

            foreach (var pos in Positions)
            {
                if (pos.Label == label && pos.Pips > Trail && pos.HasTrailingStop == false)
                    ModifyPosition(pos, vwap.VWAP.LastValue, 0, true);
            }
        }







        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
