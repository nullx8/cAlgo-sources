using System;
using cAlgo.API;
//using cAlgo.Client;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class DicordHook : Robot
    {
        [Parameter("Vertical Position", Group = "Panel alignment", DefaultValue = VerticalAlignment.Top)]
        public VerticalAlignment PanelVerticalAlignment { get; set; }

        [Parameter("Horizontal Position", Group = "Panel alignment", DefaultValue = HorizontalAlignment.Left)]
        public HorizontalAlignment PanelHorizontalAlignment { get; set; }


 
                protected override void OnStart()
        {
        Main();
 }
 
        private static HttpClient client;
        public static HttpClient Client {
            get {
                if (client == null)
                    client = new HttpClient();

                return client;
            }
        }

//        static void Main(string[] args)
        static void Main()
        {
            //https://discord.com/api/webhooks/999986692150788137/RRT7o6oEff-GfY4am5VenBwj91yweKqTINosJLURwPpi81mGUpXCec4B0uJgXEPJNFyL
            var WebHookId = "999986692150788137";
            var WebHookToken = "RRT7o6oEff-GfY4am5VenBwj91yweKqTINosJLURwPpi81mGUpXCec4B0uJgXEPJNFyL";

            const string Success = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0041_large.png?v=1369543932";
            const string Cloud = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0189_large.png?v=1369544011";
            const string Failure = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0035_large.png?v=1369543848";
            const string Meh = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0052_large.png?v=1369543755";
            const string Warning = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0016_large.png?v=1369543588";


            const string colorBlue = "1F61E6";
            const string colorGreen = "80E61F";
            const string colorRed = "E7421F";
            const string colorPurple = "C61FE6";
            const string colorYellow = "E6C71F";

            var SuccessWebHook = new
            {
                username = "Text of the message",
                content = "This is the main content section",
                avatar_url = "https://cdn.shopify.com/s/files/1/0185/5092/products/persons-0041_large.png?v=1369543932",
                embeds = new List<object>
                {
                    new
                    {
                        title = "Embed",
                        url="https://www.google.com/search?q=something",
                        description="This is the description section of the Embed, the embed has a color bar to the left side",
                        color= int.Parse(colorGreen, System.Globalization.NumberStyles.HexNumber)
                    },

                    new
                    {
                        title = "Another Embed",
                        url="https://www.google.com/search?q=somethingElse",
                        description="This is the description section of the Embed, the embed has a color bar to the left side",
                        color= int.Parse(colorPurple, System.Globalization.NumberStyles.HexNumber)
                    }
                }
            };


            string EndPoint = string.Format("https://discordapp.com/api/webhooks/{0}/{1}", WebHookId, WebHookToken);

            var content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");
//            var content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");

 //           var content ="";
            Client.PostAsync(EndPoint, content).Wait();
        }
    }
}