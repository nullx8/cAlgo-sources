using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Linq;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class VWAP_ATR_Structure_RiskBot : Robot
    {
        [Parameter("Risk Per Trade (%)", DefaultValue = 0.05)]
        public double RiskPercent { get; set; }

        [Parameter("ATR Period", DefaultValue = 14)]
        public int AtrPeriod { get; set; }

        [Parameter("Structure Lookback", DefaultValue = 20)]
        public int StructureLookback { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 1.0)]
        public double AtrMultiplier { get; set; }

        [Parameter("VWAP Range (Pips)", DefaultValue = 1)]
        public double VWAPRangePips { get; set; }

        private AverageTrueRange _atr;
        private double _vwap;

        protected override void OnStart()
        {
            _atr = Indicators.AverageTrueRange(AtrPeriod, MovingAverageType.Simple);
        }

        protected override void OnBar()
        {
            CalculateVWAP();

            double atr = _atr.Result.LastValue;
            double close = Bars.ClosePrices.Last(1);
            double vwapZone = Symbol.PipSize * VWAPRangePips;

            bool nearVWAP = Math.Abs(close - _vwap) <= vwapZone;

            double? targetHigh = null;
            double? targetLow = null;

            // Find near-term structure within ATR range
            for (int i = 2; i <= StructureLookback; i++)
            {
                double barHigh = Bars.HighPrices.Last(i);
                double barLow = Bars.LowPrices.Last(i);

                if (barHigh > close && (barHigh - close) <= (atr * AtrMultiplier))
                    targetHigh = barHigh;

                if (barLow < close && (close - barLow) <= (atr * AtrMultiplier))
                    targetLow = barLow;

                if (targetHigh.HasValue && targetLow.HasValue)
                    break;
            }

            if (!targetHigh.HasValue || !targetLow.HasValue || !nearVWAP)
                return;

            double stopLoss, takeProfit;
            TradeType direction;
            double entryPrice = close;

            if (close > _vwap)
            {
                direction = TradeType.Buy;
                stopLoss = targetLow.Value;
                takeProfit = targetHigh.Value;
            }
            else
            {
                direction = TradeType.Sell;
                stopLoss = targetHigh.Value;
                takeProfit = targetLow.Value;
            }

            // Risk Calculation
            double riskAmount = Account.Balance * (RiskPercent / 100.0);
            double stopPips = Math.Abs(entryPrice - stopLoss) / Symbol.PipSize;
            stopPips = stopPips == 0 ? 0.1 : stopPips; // Prevent div by 0

            double lotSize = (riskAmount / (stopPips * Symbol.PipValue)) / 100000.0;
            lotSize = Symbol.NormalizeVolumeInUnits(lotSize * 100000);

            if (lotSize <= 0)
                return;

            ClosePositions();

            ExecuteMarketOrder(direction, SymbolName, lotSize, "VWAP_Bot", stopPips, Math.Abs(takeProfit - entryPrice) / Symbol.PipSize);
        }

        private void CalculateVWAP()
        {
            double cumulativeTPV = 0;
            double cumulativeVolume = 0;

            int period = Bars.Count;

            for (int i = 0; i < period; i++)
            {
                double typicalPrice = (Bars.HighPrices[i] + Bars.LowPrices[i] + Bars.ClosePrices[i]) / 3;
                double volume = Bars.TickVolumes[i];

                cumulativeTPV += typicalPrice * volume;
                cumulativeVolume += volume;
            }

            _vwap = cumulativeVolume != 0 ? cumulativeTPV / cumulativeVolume : Bars.ClosePrices.Last(1);
        }

        private void ClosePositions()
        {
            foreach (var pos in Positions.FindAll("VWAP_Bot", SymbolName))
                ClosePosition(pos);
        }
    }
}
