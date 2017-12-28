using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using StravaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        Client _client;

        public MainViewModel(Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public bool IsAuthenticated { get; private set; }

        public IList<ActivityViewModel> Activities { get; } = new ObservableCollection<ActivityViewModel>();

        public async Task GetActivitiesAsync()
        {
            Dictionary<int, float> buddydistance = new Dictionary<int, float>();
            int page = 1;
            while (true)
            {
                var activities = await _client.Activities.GetAthleteActivities(page, 100);
                if(activities.Count == 0)
                {
                    return;
                }
                await AddActivities(activities, buddydistance);

                List<BuddyViewModel> buddiesList = new List<BuddyViewModel>();

                foreach (var buddyKey in buddydistance.Keys)
                {
                    buddiesList.Add(new BuddyViewModel() { Id = buddyKey, Name = string.Empty, Distance = buddydistance[buddyKey] / 1000f });
                }

                Buddys.Clear();
                var nameCounter = 0;
                foreach (var buddy in buddiesList.OrderByDescending(b => b.Distance))
                {
                    nameCounter++;
                    if (nameCounter <= 10)
                    {
                        var buddyName = await _client.Athletes.Get(buddy.Id);
                        buddy.Name = string.Format("{0} {1}", buddyName.FirstName, buddyName.LastName);
                    }
                    Buddys.Add(buddy);
                }
                page++;
                ActivityCount = Activities.Count.ToString();
                base.RaisePropertyChanged(() => ActivityCount);
            }

        }
        public string ActivityCount { get; set; }

        private async Task AddActivities(List<ActivitySummary> activities, Dictionary<int, float> buddydistance)
        {
            foreach (var activity in activities)
            {
                Activities.Add(new ActivityViewModel(activity));
                if (activity.AthleteCount <= 1) continue;

                var related = await _client.Activities.GetRelatedActivities(activity.Id);
                foreach (var other in related)
                {
                    if (buddydistance.ContainsKey(other.Athlete.Id))
                    {
                        buddydistance[other.Athlete.Id] = buddydistance[other.Athlete.Id] + activity.Distance;
                    }
                    else
                    {
                        buddydistance.Add(other.Athlete.Id, activity.Distance);
                    }
                }
            }
        }

        private RelayCommand _getActivitiesCommand;
        public RelayCommand GetActivitiesCommand
        {
            get
            {
                return _getActivitiesCommand ?? (_getActivitiesCommand = new RelayCommand(async () => await GetActivitiesAsync()));
            }
        }

        private RelayCommand _getBuddysCommand;
        private ActivityViewModel _selectedActivity;

        public RelayCommand GetBuddysCommand
        {
            get
            {
                return _getBuddysCommand ?? (_getBuddysCommand = new RelayCommand(async () => await GetBuddysAsync()));
            }
        }

        public ActivityViewModel SelectedActivity
        {
            get { return _selectedActivity; }
            set
            {
                _selectedActivity = value;
                GetBuddysAsync();
            }
        }

        public IList<BuddyViewModel> Buddys { get; } = new ObservableCollection<BuddyViewModel>();

        private async Task GetBuddysAsync()
        {
            Buddys.Clear();
            var related = await _client.Activities.GetRelatedActivities(SelectedActivity.GetId());
            foreach (var other in related)
            {
                Buddys.Add(new BuddyViewModel() { Id = other.Athlete.Id});
            }
        }
    }
}
