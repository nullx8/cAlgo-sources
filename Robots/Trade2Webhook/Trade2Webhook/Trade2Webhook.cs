/*  CTRADER GURU --> Template 1.0.8

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/ctrader-guru

Jul 29 15:25:25 sg m/html/tmp/test.php: test.php:
request Array#012(#012
    chat_id] => [ @CHATID ]#012
    [text] => #AUDUSD opened Buy position at 0.70232 for 0.01 lots, stoploss 0 takeprofit 0#012)

*/

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
    public class Trade2Webhook : Robot
    {

        public const string NAME = "Share Opened Trades To Webhook";
        public const string VERSION = "1.0.6";

        [Parameter("Webhook", Group = "Params", DefaultValue = "https://enfoid.com/api/a/swingfish/discord/ctrader-discord-webhook.php?key=YOURKEY")]
        public string Webhook { get; set; }

        [Parameter("Message", Group = "Params", DefaultValue = "{0};{1};{2};{3};{4};{5}")]
        public string Message { get; set; }

        public string PostParams = "text={0}";
    

        protected override void OnStart()
        {

            // --> Stampo nei log la versione corrente
            Print("{0} : {1}", NAME, VERSION);

            // --> Controllo se i valori sono coerenti 
            Webhook = Webhook.Trim();

            if (Webhook.Length < 1)
            {

                MessageBox.Show("Wrong 'Webhook', es. 'https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

            Message = Message.Trim();

            if (Message.Length < 1)
            {

                MessageBox.Show("Wrong 'Message', es. '#{0} opened {1} position at {2} for {3} lots'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

            PostParams = PostParams.Trim();

            if (PostParams.IndexOf("{0}") < 0)
            {

                MessageBox.Show("Wrong 'POST params', es. 'chat_id=[ @CHATID ]&text={0}'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

            // --> Registro il callback per condividere le operazioni 
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

        }

        protected override void OnStop()
        {
            // --> Rimuovo il callback registrato

            Positions.Opened -= OnPositionOpened;
            Positions.Closed -= OnPositionClosed;

        }

        public void OnPositionOpened(PositionOpenedEventArgs args)
        {
            double sl = (args.Position.StopLoss == null) ? 0 : (double)args.Position.StopLoss;
            double tp = (args.Position.TakeProfit == null) ? 0 : (double)args.Position.TakeProfit;

            string messageformat = string.Format("open;"+Message, args.Position.SymbolName, args.Position.TradeType, args.Position.EntryPrice, args.Position.VolumeInUnits, "0", "0");

            try
            {
                // --> Mi servono i permessi di sicurezza per il dominio, compreso i redirect
                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                // --> Autorizzo tutte le pagine di questo dominio
                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                MessageBox.Show(string.Format("{0}\r\nStopping cBots 'Share Opened Trades To Webhook' ...", exc.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

        }

       public void OnPositionClosed(PositionClosedEventArgs args)
        {
 //           double sl = (args.Position.StopLoss == null) ? 0 : (double)args.Position.StopLoss;
 //           double tp = (args.Position.TakeProfit == null) ? 0 : (double)args.Position.TakeProfit;

            var pnl = Math.Round(Account.Balance / (Account.Balance-args.Position.GrossProfit) * 100 - 100,3);
//            string messageformat = string.Format("close;"+Message, args.Position.SymbolName, args.Position.TradeType, args.Position.Pips, args.Position.VolumeInUnits, "$"+args.Position.GrossProfit, "0");
            string messageformat = string.Format("close;"+Message, args.Position.SymbolName, args.Position.TradeType, args.Position.Pips, args.Position.VolumeInUnits, pnl+"%", "0");

            try
            {
                // --> Mi servono i permessi di sicurezza per il dominio, compreso i redirect
                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                // --> Autorizzo tutte le pagine di questo dominio
                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                MessageBox.Show(string.Format("{0}\r\nstopping cBots 'Share Opened Trades To Webhook' ...", exc.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

        }
    }

}
