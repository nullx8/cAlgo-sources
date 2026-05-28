using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Fractingalper : Robot
    {
        [Parameter("Label", DefaultValue = "Fractingalper")]
        public string Label { get; set; }

        [Parameter("Start Hour", DefaultValue = 0.0, MinValue = 0, MaxValue = 23.5, Step = 0.5)]
        public double StartHour { get; set; }

        [Parameter("End Hour", DefaultValue = 23.5, MinValue = 0, MaxValue = 23.5, Step = 0.5)]
        public double EndHour { get; set; }

        [Parameter("Fractal Period", DefaultValue = 5)]
        public int FractalPeriod { get; set; }

        [Parameter("Min Change Pips", DefaultValue = 0.3, Step = 0.1)]
        public double MinChangePips { get; set; }

        [Parameter("Stop Order Distance Pips", DefaultValue = 0.3, Step = 0.1)]
        public double StopOrderDistance { get; set; }

        [Parameter("Volume In Units", DefaultValue = 1000)]
        public double Volume { get; set; }

        [Parameter("Take-Profit Pips", DefaultValue = 3, MinValue = 1, Step = 0.1)]
        public double TakeProfitPips { get; set; }

        [Parameter("Hedging Enabled?", DefaultValue = true)]
        public bool IsHedgingEnabled { get; set; }

        [Parameter("Hedge Distance Pips", DefaultValue = 3, MinValue = 1, Step = 0.1)]
        public double HedgeDistancePips { get; set; }

        [Parameter("Max Spread Pips", DefaultValue = 0.5, MinValue = 0, Step = 0.1)]
        public double MaxSpread { get; set; }

        private class Sequence
        {
            public List<Position> Positions { get; set; }
            public int Orders { get; set; }
        }

        private FullFractal _fractals;
        private readonly Dictionary<TradeType, PendingOrder> _stopOrder = new Dictionary<TradeType, PendingOrder> { { TradeType.Buy, null }, { TradeType.Sell, null } };
        private readonly Dictionary<TradeType, Sequence> _sequence = new Dictionary<TradeType, Sequence> { { TradeType.Buy, null }, { TradeType.Sell, null } };

        public class Swing
        {
            public int Index { get; set; }
            public double Price { get; set; }
        }

        public List<Swing> Highs = new List<Swing>();
        public List<Swing> Lows = new List<Swing>();
        private int _halfPeriod, _direction;

        protected override void OnStart()
        {
            _halfPeriod = (int)Math.Round(FractalPeriod / 2.0, MidpointRounding.ToEven);
            StopOrderDistance *= Symbol.PipSize;
            MaxSpread *= Symbol.PipSize;

            _fractals = Indicators.GetIndicator<FullFractal>(FractalPeriod, false, false, false, string.Empty, string.Empty, string.Empty);
            for (var index = 0; index < MarketSeries.Open.Count; index++)
            {
                _fractals.Calculate(index);
                var idx = index - _halfPeriod;

                if (!double.IsNaN(_fractals.highLowLink[idx]))
                {
                    if (_fractals.highLowLink[idx] == MarketSeries.High[idx])
                    {
                        if (_direction <= 0)
                        {
                            Highs.Insert(0, new Swing { Index = idx, Price = MarketSeries.High[idx] });
                            _direction = +1;
                        }
                        else
                        {
                            Highs[0].Index = idx;
                            Highs[0].Price = MarketSeries.High[idx];
                        }
                    }
                    else
                    {
                        if (_direction >= 0)
                        {
                            Lows.Insert(0, new Swing { Index = idx, Price = MarketSeries.Low[idx] });
                            _direction = -1;
                        }
                        else
                        {
                            Lows[0].Index = idx;
                            Lows[0].Price = MarketSeries.Low[idx];
                        }
                    }
                }
            }

            PendingOrders.Filled += OnPendingOrderFilled;
            Positions.Closed += OnPositionClosed;
        }

        private void OnPendingOrderFilled(PendingOrderFilledEventArgs obj)
        {
            var stopOrder = _stopOrder.Values.FirstOrDefault(x => x != null && x == obj.PendingOrder);
            if (stopOrder != null)
            {
                _stopOrder[obj.PendingOrder.TradeType] = null;
                _sequence[obj.PendingOrder.TradeType] = new Sequence
                {
                    Positions = new List<Position>
                    {
                        obj.Position
                    },
                    Orders = 1
                };

                var oppositeStopOrder = _stopOrder[obj.PendingOrder.TradeType.Opposite()];
                if (oppositeStopOrder != null)
                {
                    CancelPendingOrder(oppositeStopOrder);
                    _stopOrder[oppositeStopOrder.TradeType] = null;
                }
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs obj)
        {
            var sequence = _sequence.Values.FirstOrDefault(x => x != null && x.Positions.Contains(obj.Position));
            if (sequence != null)
            {
                _sequence[sequence.Positions[0].TradeType] = null;

                sequence.Positions.Remove(obj.Position);
                sequence.Positions.ForEach(x => ClosePosition(x));
            }
        }

        protected override void OnBar()
        {
            var index = MarketSeries.Open.Count - 2;
            _fractals.Calculate(index);

            var idx = index - _halfPeriod;
            if (!double.IsNaN(_fractals.highLowLink[idx]))
            {
                if (_fractals.highLowLink[idx] == MarketSeries.High[idx])
                {
                    if (_direction <= 0)
                    {
                        Highs.Insert(0, new Swing { Index = idx, Price = MarketSeries.High[idx] });
                        _direction = +1;
                    }
                    else
                    {
                        Highs[0].Index = idx;
                        Highs[0].Price = MarketSeries.High[idx];
                    }

                    PlaceStopOrder(TradeType.Buy, index);
                }
                else
                {
                    if (_direction >= 0)
                    {
                        Lows.Insert(0, new Swing { Index = idx, Price = MarketSeries.Low[idx] });
                        _direction = -1;
                    }
                    else
                    {
                        Lows[0].Index = idx;
                        Lows[0].Price = MarketSeries.Low[idx];
                    }

                    PlaceStopOrder(TradeType.Sell, index);
                }
            }
        }

        private void PlaceStopOrder(TradeType tradeType, int index)
        {
            var hourOfDay = MarketSeries.OpenTime[index + 1].TimeOfDay.TotalHours;
            var isTradingHour = StartHour < EndHour ? StartHour <= hourOfDay && hourOfDay < EndHour : StartHour <= hourOfDay || hourOfDay < EndHour;
            if (!isTradingHour)
            {
                return;
            }

            if (_sequence.Values.Any(x => x != null))
            {
                return;
            }

            var takeProfitPips = TakeProfitPips;
            var stopLossPips = !IsHedgingEnabled ? HedgeDistancePips + TakeProfitPips * 2 : (double?)null;

            if (tradeType == TradeType.Buy)
            {
                var changePips = Math.Round((Highs[0].Price - Highs[1].Price) / Symbol.PipSize, 1);
                if (changePips >= MinChangePips)
                {
                    var targetPrice = Math.Round(Highs[0].Price, Symbol.Digits) + StopOrderDistance;
                    if (_stopOrder[TradeType.Buy] == null)
                    {
                        _stopOrder[TradeType.Buy] = PlaceStopOrder(TradeType.Buy, Symbol, Volume, targetPrice, Label, stopLossPips, takeProfitPips, null, "Initial").PendingOrder;
                    }
                    else if (_stopOrder[TradeType.Buy].TargetPrice != targetPrice)
                    {
                        _stopOrder[TradeType.Buy].ModifyTargetPrice(targetPrice);
                    }
                }
            }
            else
            {
                var changePips = Math.Round((Lows[1].Price - Lows[0].Price) / Symbol.PipSize, 1);
                if (changePips >= MinChangePips)
                {
                    var targetPrice = Math.Round(Lows[0].Price, Symbol.Digits) - StopOrderDistance;
                    if (_stopOrder[TradeType.Sell] == null)
                    {
                        _stopOrder[TradeType.Sell] = PlaceStopOrder(TradeType.Sell, Symbol, Volume, targetPrice, Label, stopLossPips, takeProfitPips, null, "Initial").PendingOrder;
                    }
                    else if (_stopOrder[TradeType.Sell].TargetPrice != targetPrice)
                    {
                        _stopOrder[TradeType.Sell].ModifyTargetPrice(targetPrice);
                    }
                }
            }
        }

        protected override void OnTick()
        {
            if (!IsHedgingEnabled || Symbol.Spread > MaxSpread)
            {
                return;
            }

            foreach (var sequence in _sequence.Values)
            {
                if (sequence == null)
                {
                    continue;
                }

                if (sequence.Positions.Count == 1)
                {
                    if (sequence.Positions[0].Pips <= -HedgeDistancePips)
                    {
                        var volume = sequence.Positions[0].VolumeInUnits * 2 + Volume;
                        var tradeType = sequence.Positions[0].TradeType.Opposite();
                        var hedgeOrder = ExecuteMarketOrder(tradeType, Symbol, volume, Label, null, null, null, "Hedge");
                        if (hedgeOrder.IsSuccessful)
                        {
                            sequence.Positions.Add(hedgeOrder.Position);
                            sequence.Orders++;

                            AdjustTargets(sequence);
                        }
                    }
                }
                else
                {
                    var smallerPosition = sequence.Positions.OrderBy(x => x.VolumeInUnits).First();
                    var biggerPosition = sequence.Positions.OrderBy(x => x.VolumeInUnits).Last();

                    if (biggerPosition.Pips <= -TakeProfitPips)
                    {
                        var volume = smallerPosition.VolumeInUnits + biggerPosition.VolumeInUnits * 2 + Volume * sequence.Orders;
                        smallerPosition.ModifyVolume(volume);

                        sequence.Orders++;

                        AdjustTargets(sequence);
                    }
                }
            }
        }

        private void AdjustTargets(Sequence sequence)
        {
            var distancePips = Math.Round(Math.Abs(sequence.Positions[0].EntryPrice - sequence.Positions[1].EntryPrice) / Symbol.PipSize, 1);
            foreach (var position in sequence.Positions)
            {
                position.ModifyTakeProfitPips(distancePips);
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }

    public static class Functions
    {
        public static TradeType Opposite(this TradeType tradeType)
        {
            return tradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
        }
    }
}