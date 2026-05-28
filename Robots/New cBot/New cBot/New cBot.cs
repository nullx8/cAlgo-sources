using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
//using Newtonsoft.Json;

namespace cAlgo.Robots
{
 [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class NewcBot : Robot
    {
       [Parameter(DefaultValue = "https://discord.com/api/webhooks/976444784090710056/6yebOvloMt5zxzczNGakJ8pLABcvcXVgbp5UFqHMJRPGs-U2xs906n2R1VglhDfUYjs1")]
        public string webhookUrl { get; set; }

        private HttpClient _httpClient;

 //       public async Task<HttpResponseMessage> Send()
 //       {
 //           var content = new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
 //           return await _httpClient.PostAsync(webhookUrl, content);
 //       }

        // ReSharper disable once InconsistentNaming
        public async Task<HttpResponseMessage> Send(string content, string username = null, string avatarUrl = null, bool isTTS = false)
        {
            Content = content;
            Username = username;
            AvatarUrl = avatarUrl;
            IsTTS = isTTS;
            Embeds.Clear();
            if (embeds != null)
            {
                Embeds.AddRange(embeds);
            }

            return await Send();
        }
    }
}