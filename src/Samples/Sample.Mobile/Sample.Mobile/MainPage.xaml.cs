
using Sample.Mobile.Authentication;
using Sample.ViewModels;
using StravaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sample.Mobile
{
	public partial class MainPage : ContentPage
	{
        public Authenticator auth;

        public MainPage()
		{
			InitializeComponent();

            auth = CreateAuthenticator();
            var client = new StravaSharp.Client(auth);
            BindingContext = new MainViewModel(client);
		}
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            auth.Authenticate();
        }
        

        Authenticator CreateAuthenticator()
        {
            var redirectUrl = $"http://strava.ballendat.com/";
            var config = new RestSharp.Portable.OAuth2.Configuration.RuntimeClientConfiguration
            {
                IsEnabled = false,
                ClientId = Config.ClientId,
                ClientSecret = Config.ClientSecret,
                RedirectUri = redirectUrl,
                Scope = "view_private",
            };
            var client = new StravaClient(new Authentication.RequestFactory(), config);

            return new Authenticator(client);
        }
    }
}
