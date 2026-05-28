using System;
using System.Linq;
using cAlgo.API;

namespace cAlgo
{
    [Author("tmc", version = 1.1)]
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FileSystem)]
    public class EnhancedAlerts : Indicator
    {
        private string documentsPath;
        private int ordersCount, positionsCount;

        protected override void Initialize()
        {
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            ordersCount = PendingOrders.Count;
            positionsCount = Positions.Count;

            Positions.Closed += OnPositionsClosed;
            Positions.Opened += OnPositionsOpened;

            Timer.Start(TimeSpan.FromMilliseconds(100));
        }

        protected override void OnTimer()
        {
            if (PendingOrders.Count > ordersCount)
            {
                Notifications.PlaySound(GetPath("OrderPending.mp3"));
                ordersCount = PendingOrders.Count;
            }
            else if (PendingOrders.Count < ordersCount)
            {
                if (Positions.Count == positionsCount)
                {
                    Notifications.PlaySound(GetPath("OrderCanceled.mp3"));
                }
                ordersCount = PendingOrders.Count;
            }
            positionsCount = Positions.Count;
        }

        private void OnPositionsOpened(PositionOpenedEventArgs obj)
        {
            Notifications.PlaySound(GetPath("OrderFilled.mp3"));
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

                if (closingPrice >= takeProfit)
                {
                    Notifications.PlaySound(GetPath("TargetFilled.mp3"));
                }
                else if (closingPrice <= stopLoss)
                {
                    Notifications.PlaySound(GetPath("StopFilled.mp3"));
                }
                else
                {
                    Notifications.PlaySound(GetPath("OrderFilled.mp3"));
                }
            }
            else
            {
                stopLoss = position.StopLoss != null ? (double)position.StopLoss : double.PositiveInfinity;
                takeProfit = position.TakeProfit != null ? (double)position.TakeProfit : double.NegativeInfinity;

                if (closingPrice <= takeProfit)
                {
                    Notifications.PlaySound(GetPath("TargetFilled.mp3"));
                }
                else if (closingPrice >= stopLoss)
                {
                    Notifications.PlaySound(GetPath("StopFilled.mp3"));
                }
                else
                {
                    Notifications.PlaySound(GetPath("OrderFilled.mp3"));
                }
            }
        }

        private string GetPath(string soundName)
        {
            return string.Format("{0}\\cAlgo\\Sources\\Indicators\\Enhanced Alerts\\Sounds\\{1}", documentsPath, soundName);
        }

        public override void Calculate(int index)
        {
            // do nothing
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class Author : System.Attribute
    {
        public string name;
        public double version;

        public Author(string name)
        {
            this.name = name;
            version = 1.0;
        }
    }
}
