using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using StravaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
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

        public string CsvOutput
        {
            get => _csvOutput;
            set
            {
                _csvOutput = value;
                RaisePropertyChanged(()=>CsvOutput);
            }
        }

        private int page = 1;
        public bool IsSearchingBuddies { get; set; }

        public async Task GetActivitiesAsync()
        {
            Status = "requesting Activities...";
            CsvOutput = "waiting for Data...";
            //Dictionary<int, float> buddyTime = new Dictionary<int, float>();
            while (IsSearchingBuddies)
            {
                List<ActivitySummary> activities = null;
                try
                {
                    activities = await _client.Activities.GetAthleteActivities(page, 30);
                }
                catch (Exception e)
                {
                    activities = new List<ActivitySummary>();
                }
                if(activities.Count == 0)
                {
                    return;
                }
                await AddZoneData(activities);
                
                page++;
                ActivityCount = string.Format("{0} Activities processed. Continuing with page{1}", Activities.Count.ToString(), page);
                base.RaisePropertyChanged(() => ActivityCount);
            }
        }

        private void UpdateCsv()
        {
            StringBuilder csv = new StringBuilder();
            Dictionary<string, CsvData> zonesByDate = new Dictionary<string, CsvData>();
            
            foreach (var activity in Activities)
            {
                var dateString = activity.Summary.StartDate.Date.ToString("d");
                if (zonesByDate.ContainsKey(dateString))
                {
                    var csvData = zonesByDate[dateString];
                    UpdateCsvRow(activity, csvData);
                    zonesByDate[dateString] = csvData;//By Reference?
                }
                else
                {
                    var csvData = new CsvData();
                    UpdateCsvRow(activity, csvData);
                    zonesByDate.Add(dateString, csvData);
                }
            }

            //Header Row
            var csvDataFormatString = "{0};{1};{2};{3};{4};{5};{6}";

            var header = string.Format(csvDataFormatString,
                "Date",
                "Plan Nr.",
                "Wochentag",
                "Url", 
                "Anmerkung", 
                "Reg;G1;G2;EB;INT",
                "Reg;G1;G2;EB;INT"                    
            );
            csv.AppendLine(header);

            var toneKeysOrdered = zonesByDate.Keys.OrderBy(DateTime.Parse).ToList();
            
            var firstDate = DateTime.Parse(toneKeysOrdered.FirstOrDefault());
            var lastDate = DateTime.Parse(toneKeysOrdered.LastOrDefault()); 
            var dateCounter = firstDate;
            while (dateCounter <= lastDate)
            {
                dateCounter = dateCounter.AddDays(1);

                var dateKey = dateCounter.ToString("d");

                CsvData csvData = new CsvData();
                if (zonesByDate.ContainsKey(dateKey))
                {
                    csvData = zonesByDate[dateKey];                    
                }
                else
                {
                    //Leere Zeile
                    csvData.Date = dateCounter;
                }
                var runZonesCsv = ZonesToCsv(csvData.RunZones);
                var bikeZonesCsv = ZonesToCsv(csvData.BikeZones);
                var zoneString = string.Format(csvDataFormatString,
                    dateKey,
                    "",
                    csvData.Date.DayOfWeek.ToString().Substring(0, 3),
                    csvData.Url,
                    csvData.Description,
                    runZonesCsv,
                    bikeZonesCsv
                );
                csv.AppendLine(zoneString);

                //2 Leerzeilen nach Sonntag
                if (csvData.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    var emptyLine = string.Join(";", Enumerable.Range(0, 15).Select(s => string.Empty));
                    csv.AppendLine(emptyLine);
                    csv.AppendLine(emptyLine);
                }
            }
            
            CsvOutput = csv.ToString();            
        }

        private static string ZonesToCsv(ZoneData zonedata)
        {
            var zoneminutescsv = $"{zonedata.Reg};{zonedata.G1};{zonedata.G2};{zonedata.Eb};{zonedata.Int}";
            return zoneminutescsv;
        }

        private static void UpdateCsvRow(ActivityViewModel activity, CsvData csvData)
        {
            csvData.Date = activity.Summary.StartDate;

            var activityUrl = "https://www.strava.com/activities/" + activity.Summary.Id;
            if (string.IsNullOrEmpty(csvData.Url))
            {
                csvData.Url = activityUrl;
            }
            else
            {
                csvData.Url += " "+activityUrl;
            }

            if (string.IsNullOrEmpty(csvData.Description))
            {
                csvData.Description = activity.Name;
            }
            else
            {
                csvData.Description += ", " + activity.Name;
            }

            if (activity.Summary.Type == ActivityType.Ride || activity.Summary.Type == ActivityType.VirtualRide)
            {
                csvData.BikeZones.Reg += activity.Zones.Reg;
                csvData.BikeZones.G1 += activity.Zones.G1;
                csvData.BikeZones.G2 += activity.Zones.G2;
                csvData.BikeZones.Eb += activity.Zones.Eb;
                csvData.BikeZones.Int += activity.Zones.Int;
            }
            else if (activity.Summary.Type == ActivityType.Run)
            {
                csvData.RunZones.Reg += activity.Zones.Reg;
                csvData.RunZones.G1 += activity.Zones.G1;
                csvData.RunZones.G2 += activity.Zones.G2;
                csvData.RunZones.Eb += activity.Zones.Eb;
                csvData.RunZones.Int += activity.Zones.Int;
            }
            else
            {
                //TODO
                ;
            }
        }

        public string ActivityCount { get; set; }

        private async Task AddZoneData(List<ActivitySummary> activities)
        {
            foreach (var activity in activities)
            {
                var activityViewModel = new ActivityViewModel(activity);

                //if (activity.AthleteCount <= 1) continue;

                var zones = await _client.Activities.GetActivitieZones(activity.Id);

                for (var index = 0; index < zones.Count; index++)
                {                    
                    var zone = zones[index];
                    bool isValidZone = false;

                    ZoneData zonedata = new ZoneData();

                    if (!zone.SensorBased || zone.Type != "heartrate")
                    {
                        if (zone.DistributionBuckets != null && zone.DistributionBuckets.Any())
                        {
                            
                        }
                        else
                        {

                        }
                        zonedata.Reg = (int) TimeSpan.FromSeconds(activity.MovingTime).TotalMinutes;
                    }
                    else
                    {
                        isValidZone = true;
                        //Valid Zones
                        for (var i = 0; i < zone.DistributionBuckets.Count; i++)
                        {
                            var minutesInZone = (int)TimeSpan.FromSeconds(zone.DistributionBuckets[i].Time).TotalMinutes;
                            switch (i)
                            {
                                case 0:
                                    zonedata.Reg = minutesInZone;
                                    break;
                                case 1:
                                    zonedata.G1 = minutesInZone;
                                    break;
                                case 2:
                                    zonedata.G2 = minutesInZone;
                                    break;
                                case 3:
                                    zonedata.Eb = minutesInZone;
                                    break;
                                case 4:
                                    zonedata.Int = minutesInZone;
                                    break;
                                default:
                                    break;

                            }
                        }
                    }
                    
                    activityViewModel.Zones = zonedata;

                    if (isValidZone)
                    {
                        break;
                    }
                }

                Activities.Add(activityViewModel);
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

        private RelayCommand _createCsvCommand;
        public RelayCommand CreateCsvCommand
        {
            get
            {
                return _createCsvCommand ?? (_createCsvCommand = new RelayCommand(async () =>
                {
                   UpdateCsv();
                }));
            }
        }

        private ActivityViewModel _selectedActivity;
        public ActivityViewModel SelectedActivity
        {
            get { return _selectedActivity; }
            set
            {
                _selectedActivity = value;
            }
        }

        public IList<ZonesViewModel> Zones { get; } = new ObservableCollection<ZonesViewModel>();

		private string _status = string.Empty;
        private string _csvOutput;

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

    public class CsvData
    {
        public CsvData()
        {
            BikeZones = new ZoneData();
            RunZones = new ZoneData();
        }

        public string Description { get; set; }
        public string Url { get; set; }
        public ZoneData BikeZones { get; set; }
        public ZoneData RunZones { get; set; }
        public DateTime Date { get; set; }
    }

    public class ZoneData
    {
        public int Reg { get; set; }
        public int G1 { get; set; }
        public int G2 { get; set; }
        public int Eb { get; set; }
        public int Int { get; set; }
    }
}
