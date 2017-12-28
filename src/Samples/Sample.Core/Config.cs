using StravaSharp;

namespace Sample
{
    public class Config
    {
        public static string ClientId => "3005";
        public static string ClientSecret => "c85a4d476f0b5391c9d62c2c4c81130fb392ae06";

        public static StravaClient CreateOAuth2Cient(string redirectUrl)
        {
            var config = new RestSharp.Portable.OAuth2.Configuration.RuntimeClientConfiguration
            {
                IsEnabled = false,
                ClientId = Config.ClientId,
                ClientSecret = Config.ClientSecret,
                RedirectUri = redirectUrl,
                Scope = "write,view_private",
            };
            return new StravaClient(new Authentication.RequestFactory(), config);
        }
    }
}