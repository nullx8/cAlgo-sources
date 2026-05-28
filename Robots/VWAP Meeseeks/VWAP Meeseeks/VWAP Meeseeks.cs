/* -------------------------------------------------------------------------------
 * 
 *    Semi-automated trading strategy placing limit order at Result which is updated on each new bar.
 *    
 *    Author: Mario Hennenberger <mario@enfoid.com>
 *    Developer: Poshtrader Ltd <info@poshtrader.com>, Jiri Beloch <jiri@poshtrader.com>
 *    
 *    Changelog:
 *      1.1.0 (April 02, 2019)
 *          - Added deviations to VWAP indicator
 *          - Added take-profit and stop-loss
 *
 *      1.0.0 (March 31, 2019)
 *          - Released
 *          
 * -------------------------------------------------------------------------------
 */

using System;
using cAlgo.API;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VWAPMeeseeks : Robot
    {
        [Parameter("Risk %", DefaultValue = 0.12, Step = 0.01)]
        public double RiskP { get; set; }

        [Parameter("Distance (Pips)", DefaultValue = 1, Step = 0.1)]
        public double Distance { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 1, Step = 0.1)]
        public double StopLossPips { get; set; }

        [Parameter("Hedge Instead (dev)", DefaultValue = true)]
        public bool HedgeInstead { get; set; }

        [Parameter("Profit Deviation", DefaultValue = 2.8, Step = 0.1)]
        public double ProfitDeviation { get; set; }

        private VWAP _vwap;
        private PendingOrder _limitOrder;

        /// <summary>
        /// Called when cBot is started
        /// </summary>
        protected override void OnStart()
        {
            Distance *= Symbol.PipSize;

            _vwap = Indicators.GetIndicator<VWAP>(ProfitDeviation);

            var tradeType = Symbol.Bid > _vwap.Result.Last(1) ? TradeType.Buy : TradeType.Sell;
            /*
            var tradeTypeHedge = Symbol.Bid < _vwap.Result.Last(1) ? TradeType.Buy : TradeType.Sell;
            var targetPrice = GetTargetPrice(tradeType);
            var takeProfitPips = GetDistanceInPips(targetPrice, tradeType == TradeType.Buy ? _vwap.Upper.Last(1) : _vwap.Lower.Last(1));
//            var order = PlaceLimitOrder(tradeType, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice, "Result", StopLossPips, takeProfitPips);
            var order = PlaceLimitOrder(tradeType, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice, "Result", StopLossPips, takeProfitPips);
            var orderHedge = PlaceLimitOrder(tradeTypeHedge, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice - StopLossPips, "ResultH");
            if ((order.IsSuccessful) && (orderHedge.IsSuccessful))
            {
                _limitOrder = order.PendingOrder;
                _limitOrder = orderHedge.PendingOrder;
            }
            else
*/
            var tradeTypeHedge = Symbol.Bid < _vwap.Result.Last(1) ? TradeType.Buy : TradeType.Sell;
            var targetPrice = GetTargetPrice(tradeType);
            var takeProfitPips = GetDistanceInPips(targetPrice, tradeType == TradeType.Buy ? _vwap.Upper.Last(1) : _vwap.Lower.Last(1));
            var order = PlaceLimitOrder(tradeType, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice, "Result", StopLossPips, takeProfitPips);
//            var orderHedge = PlaceStopOrder(tradeTypeHedge, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice - StopLossPips, "ResultH");
            if (order.IsSuccessful)
            {
                _limitOrder = order.PendingOrder;
            }
            else
            {
                Print(order.Error);
                Stop();
                return;
            }

            PendingOrders.Filled += args => CheckForStop(args.PendingOrder);
            PendingOrders.Cancelled += args => CheckForStop(args.PendingOrder);
        }

        /// Default Sizing Function
        /// GetRiskSize(_pips, _risk)
        private double GetRiskSize(double pips, double risk = 1)
        {
            return Math.Min(Symbol.NormalizeVolumeInUnits((Account.Balance * risk / 100) / (pips * Symbol.PipValue), RoundingMode.Down), Symbol.VolumeInUnitsMax);
        }


        /// <summary>
        /// Stops the cBot if the limit order was filled or cancelled
        /// </summary>
        private void CheckForStop(PendingOrder order)
        {
            if (order != _limitOrder)
                return;

            _limitOrder = null;
            Stop();
        }

        /// <summary>
        /// Called on each new bar
        /// </summary>
        protected override void OnBar()
        {
            if (_limitOrder == null)
                return;

            var newTargetPrice = GetTargetPrice(_limitOrder.TradeType);
            var newTakeProfitPips = GetDistanceInPips(newTargetPrice, _limitOrder.TradeType == TradeType.Buy ? _vwap.Upper.Last(1) : _vwap.Lower.Last(1));
            if (Math.Abs(_limitOrder.TargetPrice - newTargetPrice) >= Symbol.TickSize || Math.Abs(_limitOrder.TakeProfitPips.Value - newTakeProfitPips) >= 0.1)
            {
                ModifyPendingOrder(_limitOrder, newTargetPrice, StopLossPips, newTakeProfitPips);
            }
        }

        /// <summary>
        /// Calculates limit order target price based on Result
        /// </summary>
        private double GetTargetPrice(TradeType tradeType)
        {
            switch (tradeType)
            {
                case TradeType.Buy:
                    return Math.Round(_vwap.Result.Last(1) + Distance, Symbol.Digits);
                case TradeType.Sell:
                    return Math.Round(_vwap.Result.Last(1) - Distance, Symbol.Digits);
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Calculates distance between two prices in pips
        /// </summary>
        private double GetDistanceInPips(double price1, double price2)
        {
            return Math.Round(Math.Abs(price1 - price2) / Symbol.PipSize, 1);
        }

        /// <summary>
        /// Called when cBot is stopped
        /// </summary>
        protected override void OnStop()
        {
            if (_limitOrder != null)
            {
                CancelPendingOrder(_limitOrder);
            }
        }
    }
}
