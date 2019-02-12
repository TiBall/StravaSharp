
using Sample.Mobile.Authentication;
using Sample.Mobile.ViewModels;
using Sample.ViewModels;
using StravaSharp;
using Xamarin.Forms;

namespace Sample.Mobile
{
    public partial class MainPage : ContentPage
	{
        public MainPage()
		{
			InitializeComponent();       
			//BindingContext = new MobileMainViewModel();     
		}

	    protected override void OnAppearing()
	    {
	        base.OnAppearing();
	        
            ViewModelLocator.Instance.MainViewModel.GetUpdateCommand.Execute(null);
        }
	}
}
