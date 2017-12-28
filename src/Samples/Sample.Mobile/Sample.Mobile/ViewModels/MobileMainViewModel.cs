﻿using GalaSoft.MvvmLight.Command;
using Sample.Mobile.Authentication;
using Sample.ViewModels;
using StravaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Mobile.ViewModels
{
    public class MobileMainViewModel:MainViewModel
    {
        private static Authenticator auth = CreateAuthenticator();

        public MobileMainViewModel():base(new Client(auth))
        {
            UpdateIsAuthenticated();
        }

        static Authenticator CreateAuthenticator()
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
            var client = new StravaClient(new RequestFactory(), config);

            return new Authenticator(client);
        }

        private RelayCommand _authCommand;
        public RelayCommand GetAuthentificationCommand
        {
            get
            {
                return _authCommand ?? (_authCommand = new RelayCommand(async () => await AuthenticateAsync()));
            }
        }

        private async Task AuthenticateAsync()
        {
            await auth.Authenticate();
            UpdateIsAuthenticated();
        }


        private RelayCommand _updateCommand;
        public RelayCommand GetUpdateCommand
        {
            get
            {
                return _updateCommand ?? (_updateCommand = new RelayCommand(() => UpdateIsAuthenticated()));
            }
        }
        private void UpdateIsAuthenticated()
        {
            IsAuthenticated = auth.IsAuthenticated;
            this.RaisePropertyChanged(() => IsAuthenticated);
        }
    }
}
