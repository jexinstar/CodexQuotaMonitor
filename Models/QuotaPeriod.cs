using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CodexQuotaMonitor.Models
{
    public sealed class QuotaPeriod : INotifyPropertyChanged
    {
        private string _name;
        private double _used;
        private double _limit;
        private string _unit;
        private string _accentColor;
        private long? _windowDurationMinutes;
        private DateTime? _resetAt;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public double Used
        {
            get { return _used; }
            set
            {
                if (SetProperty(ref _used, value))
                {
                    OnPropertyChanged("UsagePercentage");
                    OnPropertyChanged("Remaining");
                }
            }
        }

        public double Limit
        {
            get { return _limit; }
            set
            {
                if (SetProperty(ref _limit, value))
                {
                    OnPropertyChanged("UsagePercentage");
                    OnPropertyChanged("Remaining");
                }
            }
        }

        public string Unit
        {
            get { return _unit; }
            set { SetProperty(ref _unit, value); }
        }

        public string AccentColor
        {
            get { return _accentColor; }
            set { SetProperty(ref _accentColor, value); }
        }

        public long? WindowDurationMinutes
        {
            get { return _windowDurationMinutes; }
            set { SetProperty(ref _windowDurationMinutes, value); }
        }

        public DateTime? ResetAt
        {
            get { return _resetAt; }
            set { SetProperty(ref _resetAt, value); }
        }

        public double UsagePercentage
        {
            get
            {
                if (Limit <= 0
                    || double.IsNaN(Limit)
                    || double.IsInfinity(Limit)
                    || double.IsNaN(Used)
                    || double.IsInfinity(Used))
                {
                    return 0;
                }

                return Math.Max(0, Math.Min(Used / Limit * 100.0, 100.0));
            }
        }

        public double Remaining
        {
            get
            {
                if (double.IsNaN(Limit)
                    || double.IsInfinity(Limit)
                    || double.IsNaN(Used)
                    || double.IsInfinity(Used))
                {
                    return 0;
                }

                return Math.Max(Limit - Used, 0);
            }
        }

        private bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
