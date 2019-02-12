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

        public bool IsAuthenticated { get; protected set; }

        public IList<ActivityViewModel> Activities { get; } = new ObservableCollection<ActivityViewModel>();

        private Dictionary<int, Athlete> allMyBuddies = new Dictionary<int, Athlete>();
        private Dictionary<int, float> buddydistance = new Dictionary<int, float>();

        private int page = 1;
        public bool IsSearchingBuddies { get; set; }

        public async Task GetActivitiesAsync()
        {
            Status = "requesting Activities...";
            //Dictionary<int, float> buddyTime = new Dictionary<int, float>();
            while (IsSearchingBuddies)
            {
                var activities = await _client.Activities.GetAthleteActivities(page, 30);
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
                     
                    if (!allMyBuddies.ContainsKey(buddy.Id))
                    {
                        allMyBuddies.Add(buddy.Id, await _client.Athletes.Get(buddy.Id));
                    }

                    Athlete buddyData = allMyBuddies[buddy.Id];
                    buddy.Name = string.Format("{0} {1}", buddyData.FirstName, buddyData.LastName);
                    Buddys.Add(buddy);
                }
                page++;
                ActivityCount = string.Format("{0} Activities processed", Activities.Count.ToString());
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
                return _getActivitiesCommand ?? (_getActivitiesCommand = new RelayCommand(async () =>
                {
                    if (IsSearchingBuddies)
                    {
                        IsSearchingBuddies = false;
                    }
                    else
                    {
                        IsSearchingBuddies = true;
                        await GetActivitiesAsync();
                    }
                }));
            }
        }

        private RelayCommand _getBuddysCommand;
        public RelayCommand GetBuddysCommand
        {
            get
            {
                return _getBuddysCommand ?? (_getBuddysCommand = new RelayCommand(async () => await GetBuddysAsync()));
            }
        }

        private ActivityViewModel _selectedActivity;
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
		
		private string _status = string.Empty;

        /// <summary>
        /// Sets and gets the Status property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                Set(()=>Status, ref _status, value);
            }
        }
    }
}
