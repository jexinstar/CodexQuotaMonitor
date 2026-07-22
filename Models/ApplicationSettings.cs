using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace CodexQuotaMonitor.Models
{
    [DataContract]
    public sealed class ApplicationSettings : INotifyPropertyChanged
    {
        private bool _autoRefreshEnabled;
        private int _refreshIntervalMinutes;
        private bool _alwaysOnTop;
        private bool _startWithWindows;
        private bool _compactMode;
        private bool _darkMode;
        private bool _showTrendChart;
        private double _backgroundOpacity;
        private LocalDataSourceType _dataSourceType;

        public ApplicationSettings()
        {
            SetDefaults();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [DataMember(Order = 1)]
        public bool AutoRefreshEnabled
        {
            get { return _autoRefreshEnabled; }
            set { SetProperty(ref _autoRefreshEnabled, value); }
        }

        [DataMember(Order = 2)]
        public int RefreshIntervalMinutes
        {
            get { return _refreshIntervalMinutes; }
            set
            {
                SetProperty(
                    ref _refreshIntervalMinutes,
                    NormalizeRefreshInterval(value));
            }
        }

        [DataMember(Order = 3)]
        public bool AlwaysOnTop
        {
            get { return _alwaysOnTop; }
            set { SetProperty(ref _alwaysOnTop, value); }
        }

        [DataMember(Order = 4)]
        public bool StartWithWindows
        {
            get { return _startWithWindows; }
            set { SetProperty(ref _startWithWindows, value); }
        }

        [DataMember(Order = 5)]
        public bool CompactMode
        {
            get { return _compactMode; }
            set { SetProperty(ref _compactMode, value); }
        }

        [DataMember(Order = 6)]
        public bool DarkMode
        {
            get { return _darkMode; }
            set { SetProperty(ref _darkMode, value); }
        }

        [DataMember(Order = 7)]
        public bool ShowTrendChart
        {
            get { return _showTrendChart; }
            set { SetProperty(ref _showTrendChart, value); }
        }

        [DataMember(Order = 8)]
        public double BackgroundOpacity
        {
            get { return _backgroundOpacity; }
            set
            {
                SetProperty(
                    ref _backgroundOpacity,
                    NormalizeBackgroundOpacity(value));
            }
        }

        [DataMember(Order = 9)]
        public LocalDataSourceType DataSourceType
        {
            get { return _dataSourceType; }
            set
            {
                SetProperty(
                    ref _dataSourceType,
                    value == LocalDataSourceType.CodexSessionLogs
                        ? value
                        : LocalDataSourceType.Mock);
            }
        }

        public ApplicationSettings Clone()
        {
            var clone = new ApplicationSettings();
            clone.CopyFrom(this);
            return clone;
        }

        public void CopyFrom(ApplicationSettings source)
        {
            if (source == null)
            {
                return;
            }

            AutoRefreshEnabled = source.AutoRefreshEnabled;
            RefreshIntervalMinutes = source.RefreshIntervalMinutes;
            AlwaysOnTop = source.AlwaysOnTop;
            StartWithWindows = source.StartWithWindows;
            CompactMode = source.CompactMode;
            DarkMode = source.DarkMode;
            ShowTrendChart = source.ShowTrendChart;
            BackgroundOpacity = source.BackgroundOpacity;
            DataSourceType = source.DataSourceType;
        }

        public static bool IsSupportedRefreshInterval(int minutes)
        {
            return minutes == 5
                   || minutes == 15
                   || minutes == 30
                   || minutes == 60;
        }

        public static int NormalizeRefreshInterval(int minutes)
        {
            return IsSupportedRefreshInterval(minutes) ? minutes : 15;
        }

        public static double NormalizeBackgroundOpacity(double opacity)
        {
            if (double.IsNaN(opacity) || double.IsInfinity(opacity))
            {
                return 0.70;
            }

            double clamped = System.Math.Max(
                0.10,
                System.Math.Min(0.90, opacity));
            return System.Math.Round(
                clamped * 10.0,
                System.MidpointRounding.AwayFromZero) / 10.0;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            _autoRefreshEnabled = false;
            _refreshIntervalMinutes = 15;
            _alwaysOnTop = false;
            _startWithWindows = false;
            _compactMode = false;
            _darkMode = true;
            _showTrendChart = true;
            _backgroundOpacity = 0.70;
            _dataSourceType = LocalDataSourceType.CodexSessionLogs;
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

        private void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
