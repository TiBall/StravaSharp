using System;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using StravaSharp;

namespace Sample.ViewModels
{
    public class ActivityViewModel
    {
        private ActivitySummary _summary;
        private ZoneData _zones= new ZoneData();

        public ZoneData Zones
        {
            get => _zones;
            set
            {
                _zones = value;
            }
        }

        public ActivitySummary Summary
        {
            get => _summary;
        }

        public ActivityViewModel(ActivitySummary summary)
        {
            _summary = summary;
        }

        public string Name => _summary.Name;

        public string Start => _summary.StartDate.ToString("yyyy.MM.dd hh:mm");

        public string Distance
        {
            get
            {
                if (_summary.Distance > 1000)
                    return $"{(_summary.Distance / 1000.0f).ToString("F2")} km";
                else
                    return $"{_summary.Distance} m";
            }
        }

        public string ZonesString
        {
            get => _zones==null?string.Empty: $"Req: {_zones.Reg}min, G1:{_zones.G1}min, G2:{_zones.G2}min, Eb:{_zones.Eb}min, Int:{_zones.Int}min";
        }

        public long GetId() { return _summary.Id; }
    }
}