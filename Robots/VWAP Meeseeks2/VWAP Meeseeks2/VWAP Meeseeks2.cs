#region auth and using
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
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

#endregion

namespace cAlgo
{
    [Robot(AccessRights = AccessRights.None)]
    public class VWAPMeeseeks2 : Robot
    {
        #region params and variables
        [Parameter("Trigger (Pips)", Group = "Execution", DefaultValue = 1, Step = 0.1)]
        public double Distance { get; set; }

        [Parameter("Profit (Deviation)", Group = "Execution", DefaultValue = 2.8, Step = 0.1)]
        public double ProfitDeviation { get; set; }

        [Parameter("Risk %", Group = "Risk", DefaultValue = 0.12, Step = 0.01)]
        public double RiskP { get; set; }

        [Parameter("Min RR", Group = "Risk", DefaultValue = 4, Step = 0.1)]
        public double minRR { get; set; }

        [Parameter("Stop (Pips)", Group = "Risk", DefaultValue = 0.1, Step = 0.1)]
        public double StopLossPips { get; set; }

        [Parameter("Stop (Deviation)", Group = "Risk", DefaultValue = 0.5, Step = 0.01)]
        public double LossDeviation { get; set; }

        [Parameter("GMT +/- Hours", Group = "Timeout", DefaultValue = 8, Step = 1)]
        public int correction { get; set; }
        [Parameter("Start Hour", Group = "Timeout", DefaultValue = 7, MinValue = 0, MaxValue = 23)]
        public int startHour { get; set; }
        [Parameter("End Hour", Group = "Timeout", DefaultValue = 13, MinValue = 0, MaxValue = 23)]
        public int endHour { get; set; }



        [Parameter("Hedge Instead", Group = "dev (Not Working)", DefaultValue = true)]
        public bool HedgeInstead { get; set; }

        [Parameter("be a true Meeseeks", Group = "dev (Not Working)", DefaultValue = false)]
        public bool trueMeeseeks { get; set; }



        private double cRR = 0;
        private Colors _Colors;
        private double stopLossPips;
        private VWAP _vwap, _vwapStop;
        // 0 = sleeping, 1 = waiting/normal, 2 = order placed 3 = intrade, 5 = toosmall, 9 = error
        private int execStatus = 0;
        private PendingOrder _limitOrder;

        string Screentext = "\nvWap Meeseeks";

        protected override void OnStart()
        {
            OnBar();
        }
        #endregion
        protected override void OnTick()
        {
            var pos = Positions.Any<Position>(e => SymbolName == e.SymbolName);
            var ord = PendingOrders.Any<PendingOrder>(e => SymbolName == e.SymbolName);

            // Asset Trading
            if (!TimeFilterCheck() && startHour != 0 && endHour != 0)
            {
                PrintText(": Sleeping", "Red");
            }
            else
            {
                if (!pos && ord)
                {
                    PrintText(": Wubba Lubba Dub Dub! " + Math.Round(cRR, 1) + " RR", "Aqua");
                }
                else if (pos)
                {
                    PrintText(": To Live Is To Risk It All ...", "Orange");
                }
                else if (!pos && !ord)
                {
                    PrintText(": No Order or Trade .. somethings is wrong", "White");
                }


            }
        }

        protected override void OnBar()
        {
            if (_limitOrder != null)
                Modify();

            var pos = Positions.Any<Position>(e => SymbolName == e.SymbolName);
            var ord = PendingOrders.Any<PendingOrder>(e => SymbolName == e.SymbolName);

            if (!pos && !ord && (TimeFilterCheck() || (startHour == 0 && endHour == 0)))
            {
                PlaceOrder();
            }

            if (!TimeFilterCheck() && ord)
            {
                PrintText(": Sleep Prep Caceling Orders", "White");
                foreach (var ordd in PendingOrders)
                    if (ordd.SymbolName == SymbolName)
                        CancelPendingOrder(ordd);
            }
        }



        private void PrintText(string txt, string _Color)
        {
            Enum.TryParse<Colors>(_Color, out _Colors);
            ChartObjects.DrawText("VWAPMeeseeks2", "\nvWap Meeseeks" + txt, StaticPosition.TopLeft, _Colors);
        }

        #region >>> Place Order
        private void PlaceOrder()
        {

            Distance *= Symbol.PipSize;
            _vwap = Indicators.GetIndicator<VWAP>(ProfitDeviation);
            if (LossDeviation != 0)
            {
                _vwapStop = Indicators.GetIndicator<VWAP>(LossDeviation);
            }
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
            if (LossDeviation == 0)
            {
                stopLossPips = StopLossPips;
            }
            else
            {
                stopLossPips = (GetDistanceInPips(targetPrice, tradeType == TradeType.Buy ? _vwapStop.Lower.Last(1) : _vwapStop.Upper.Last(1)) + StopLossPips);
            }

            cRR = (takeProfitPips / stopLossPips);
            if ((takeProfitPips / stopLossPips) > minRR)
            {

                var order = PlaceLimitOrder(tradeType, Symbol, GetRiskSize(stopLossPips, RiskP), targetPrice, "Meeseecks Limit", stopLossPips, takeProfitPips);
//            var order = PlaceLimitOrder(tradeType, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice, "Result", StopLossPips, takeProfitPips);
                //            var orderHedge = PlaceStopOrder(tradeTypeHedge, Symbol, GetRiskSize(StopLossPips, RiskP), targetPrice - StopLossPips, "ResultH");
                if (order.IsSuccessful)
                {
                    _limitOrder = order.PendingOrder;
                }
                else
                {
                    Print(order.Error);
                    // Stop();
                    return;
                }
            }
            else
            {
                PrintText(": Love Is Just A Chemical Reaction - NO Trade!! " + (takeProfitPips / stopLossPips) + " " + minRR, "Red");
            }
            // PendingOrders.Filled += args => CheckForStop(args.PendingOrder);
            // PendingOrders.Cancelled += args => CheckForStop(args.PendingOrder);
        }
        #endregion

        #region >>> Modify Order
        private void Modify()
        {
            var newTargetPrice = GetTargetPrice(_limitOrder.TradeType);
            var newTakeProfitPips = GetDistanceInPips(newTargetPrice, _limitOrder.TradeType == TradeType.Buy ? _vwap.Upper.Last(1) : _vwap.Lower.Last(1));
            if (Math.Abs(_limitOrder.TargetPrice - newTargetPrice) >= Symbol.TickSize || Math.Abs(_limitOrder.TakeProfitPips.Value - newTakeProfitPips) >= 0.1)
            {
                ModifyPendingOrder(_limitOrder, newTargetPrice, stopLossPips, newTakeProfitPips);
            }
        }
        #endregion

        #region =================  FUNCTIONS  =====================

        private bool TimeFilterCheck()
        {
            var t = Bars.OpenTimes.Last(0).AddHours(correction).Hour;
            //Print("Time Now : " + t);
            if (t >= startHour && t < endHour)
            {
                execStatus = 1;
                return true;
            }
            else
                execStatus = 0;
            return false;
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
        #endregion

        #region >>> On Stop
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
        #endregion
    }
}
