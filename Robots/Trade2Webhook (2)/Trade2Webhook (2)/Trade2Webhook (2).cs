using System;
using System.Net;
using cAlgo.API;
using System.Windows.Forms;
using cAlgo.API.Internals;
using System.Text.RegularExpressions;

namespace cAlgo
{

    // --> Estensioni che rendono il codice più leggibile
    #region Extensions

    /// <summary>
    /// Estensione che fornisce metodi aggiuntivi per il simbolo
    /// </summary>
    public static class SymbolExtensions
    {

        /// <summary>
        /// Converte il numero di pips corrente da digits a double
        /// </summary>
        /// <param name="Pips">Il numero di pips nel formato Digits</param>
        /// <returns></returns>
        public static double DigitsToPips(this Symbol MySymbol, double Pips)
        {

            return Math.Round(Pips / MySymbol.PipSize, 2);

        }

        /// <summary>
        /// Converte il numero di pips corrente da double a digits
        /// </summary>
        /// <param name="Pips">Il numero di pips nel formato Double (2)</param>
        /// <returns></returns>
        public static double PipsToDigits(this Symbol MySymbol, double Pips)
        {

            return Math.Round(Pips * MySymbol.PipSize, MySymbol.Digits);

        }

    }

    /// <summary>
    /// Estensione che fornisce metodi aggiuntivi per le Bars
    /// </summary>
    public static class BarsExtensions
    {

        /// <summary>
        /// Converte l'indice di una bar partendo dalla data di apertura
        /// </summary>
        /// <param name="MyTime">La data e l'ora di apertura della candela</param>
        /// <returns></returns>
        public static int GetIndexByDate(this Bars MyBars, DateTime MyTime)
        {

            for (int i = MyBars.ClosePrices.Count - 1; i >= 0; i--)
            {

                if (MyTime == MyBars.OpenTimes[i])
                    return i;

            }

            return -1;

        }

    }

    #endregion

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Trade2Webhook2 : Robot
    {

        public const string NAME = "Share Opened Trades To Webhook";
        public const string VERSION = "1.0.6";

//        [Parameter("Webhook", Group = "Params", DefaultValue = "https://enfoid.com/api/a/swingfish/discord/ctrader-discord-webhook.php?key=YOURKEY")]
        [Parameter("Webhook", Group = "Params", DefaultValue = "https://enfoid.com/api/a/swingfish/discord/ctrader-discord-webhook.php")]
        public string Webhook { get; set; }

        [Parameter("Message", Group = "Params", DefaultValue = "{0};{1};{2};{3};{4};{5}")]
        public string Message { get; set; }

        public string PostParams = "text={0}";
    

        protected override void OnStart()
        {
            // Subscribe to events.
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
        }

        protected override void OnStop()
        {
            // Unsubscribe to events.
            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;
        }

        public void OnPositionOpened(PositionOpenedEventArgs args)
        {
 //           var endpoint = "htpp://api.server.com/positions/add";
            var data = string.Format("{0};{1};{2}", args.Position.Id, args.Position.EntryPrice, args.Position.EntryTime);

            var response = new WebClient().UploadString(Webhook, data);
        }

        public void OnPositionClosed(PositionClosedEventArgs args)
        {
        }
    }
}