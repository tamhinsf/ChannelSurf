using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;


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
    }
}
