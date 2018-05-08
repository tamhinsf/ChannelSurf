using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChannelSurfCli.Utils
{
    public class O365
    {
        public const string MsGraphEndpoint = "https://graph.microsoft.com/v1/";
        public const string MsGraphBetaEndpoint = "https://graph.microsoft.com/beta/";

        public class AuthenticationHelper : IAuthenticationProvider
        {
            public string AccessToken { get; set; }

            public Task AuthenticateRequestAsync(HttpRequestMessage request)
            {
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                return Task.FromResult(0);
            }
        }

        public static string getUserGuid(string aadAccessToken, string userUpn)
        {
            Helpers.httpClient.DefaultRequestHeaders.Clear();
            Helpers.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aadAccessToken);
            Helpers.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var httpResponseMessage =
                    Helpers.httpClient.GetAsync(O365.MsGraphBetaEndpoint + userUpn + "/id").Result;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var httpResultString = httpResponseMessage.Content.ReadAsStringAsync().Result;
                JObject userObject = JObject.Parse(httpResultString);
                return (string)userObject["value"];
            }

            return "";

        }
    }
}
