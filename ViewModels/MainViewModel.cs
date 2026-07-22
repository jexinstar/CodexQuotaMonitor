using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CodexQuotaMonitor.Commands;
using CodexQuotaMonitor.Models;
using CodexQuotaMonitor.Services;

namespace CodexQuotaMonitor.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IDisposable
    {
        private IQuotaService _quotaService;
        private readonly SettingsService _settingsService;
        private readonly ApplicationSettings _settings;
        private readonly DispatcherTimer _autoRefreshTimer;
        private readonly ReadOnlyCollection<TimeSpan> _refreshIntervalOptions;
        private QuotaSummary _quotaSummary;
        private string _status;
        private SyncStatusType _statusType;
        private DateTime _lastSyncTime;
        private bool _isRefreshing;
        private bool _autoRefreshEnabled;
        private TimeSpan _refreshInterval;
        private bool _isDisposed;
        private bool _isApplyingSettings;
        private int _dataSourceGeneration;
        private bool _refreshPending;

        public MainViewModel(IQuotaService quotaService)
            : this(quotaService, new SettingsService())
        {
        }

        public MainViewModel(
            IQuotaService quotaService,
            SettingsService settingsService)
            : this(
                quotaService,
                settingsService,
                settingsService == null ? null : settingsService.Load())
        {
        }

        public MainViewModel(
            IQuotaService quotaService,
            SettingsService settingsService,
            ApplicationSettings settings)
        {
            if (quotaService == null)
            {
                throw new ArgumentNullException("quotaService");
            }

            if (settingsService == null)
            {
                throw new ArgumentNullException("settingsService");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            _quotaService = quotaService;
            _settingsService = settingsService;
            _settings = settings;
            _settings.PropertyChanged += Settings_OnPropertyChanged;
            _status = "未同步";
            _statusType = SyncStatusType.Syncing;
            _refreshInterval = TimeSpan.FromMinutes(15);
            _refreshIntervalOptions = new List<TimeSpan>
            {
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(60)
            }.AsReadOnly();

            _autoRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = _refreshInterval
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_OnTick;

            RefreshCommand = new RelayCommand(
                async parameter => await RefreshAsync(parameter as string ?? "manual"),
                parameter => !IsRefreshing);
            OpenSettingsCommand = new RelayCommand(
                parameter => RaiseOpenSettingsRequested());

            ApplyApplicationSettings();
            RefreshCommand.Execute("startup");
        }

        public event EventHandler OpenSettingsRequested;

        public QuotaSummary QuotaSummary
        {
            get { return _quotaSummary; }
            private set
            {
                if (SetProperty(ref _quotaSummary, value))
                {
                    OnPropertyChanged("TodayQuota");
                    OnPropertyChanged("WeekQuota");
                    OnPropertyChanged("MonthQuota");
                    OnPropertyChanged("RemainingQuota");
                    OnPropertyChanged("Trend");
                    OnPropertyChanged("AccountDisplay");
                    OnPropertyChanged("FiveHourResetDisplay");
                    OnPropertyChanged("SevenDayResetDisplay");
                    OnPropertyChanged("FiveHourQuotaSubtitle");
                }
            }
        }

        public QuotaPeriod TodayQuota
        {
            get { return QuotaSummary == null ? null : QuotaSummary.TodayQuota; }
        }

        public QuotaPeriod WeekQuota
        {
            get { return QuotaSummary == null ? null : QuotaSummary.WeekQuota; }
        }

        public QuotaPeriod MonthQuota
        {
            get { return QuotaSummary == null ? null : QuotaSummary.MonthQuota; }
        }

        public QuotaPeriod RemainingQuota
        {
            get { return QuotaSummary == null ? null : QuotaSummary.RemainingQuota; }
        }

        public ObservableCollection<UsageTrendPoint> Trend
        {
            get
            {
                return QuotaSummary == null
                    ? null
                    : QuotaSummary.Trend;
            }
        }

        public string AccountDisplay
        {
            get
            {
                CodexAccountInfo account = QuotaSummary == null
                    ? null
                    : QuotaSummary.Account;
                if (account == null)
                {
                    return QuotaSummary != null
                           && QuotaSummary.RequiresOpenAiAuth == true
                        ? "未登录"
                        : "未提供";
                }

                if (!string.IsNullOrWhiteSpace(account.PlanType))
                {
                    return account.PlanType;
                }

                if (!string.IsNullOrWhiteSpace(account.Type))
                {
                    return account.Type;
                }

                return account.RequiresOpenAiAuth == true
                    ? "未登录"
                    : "已连接";
            }
        }

        public string FiveHourResetDisplay
        {
            get
            {
                return FormatResetTime(
                    "5h",
                    QuotaSummary == null
                        ? null
                        : QuotaSummary.FiveHourResetTime);
            }
        }

        public string SevenDayResetDisplay
        {
            get
            {
                return FormatResetTime(
                    "7d",
                    QuotaSummary == null
                        ? null
                        : QuotaSummary.SevenDayResetTime);
            }
        }

        public string FiveHourQuotaSubtitle
        {
            get { return IsQuotaAvailable(TodayQuota) ? "剩余" : "未提供"; }
        }

        public string Status
        {
            get { return _status; }
            private set { SetProperty(ref _status, value); }
        }

        public SyncStatusType StatusType
        {
            get { return _statusType; }
            private set { SetProperty(ref _statusType, value); }
        }

        public DateTime LastSyncTime
        {
            get { return _lastSyncTime; }
            private set
            {
                if (SetProperty(ref _lastSyncTime, value))
                {
                    OnPropertyChanged("LastSyncTimeDisplay");
                }
            }
        }

        public string LastSyncTimeDisplay
        {
            get
            {
                return LastSyncTime == DateTime.MinValue
                    ? "等待同步"
                    : LastSyncTime.ToString(
                        "yyyy/MM/dd HH:mm:ss",
                        CultureInfo.InvariantCulture);
            }
        }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            private set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool AutoRefreshEnabled
        {
            get { return _autoRefreshEnabled; }
            set
            {
                if (SetProperty(ref _autoRefreshEnabled, value))
                {
                    if (!_isApplyingSettings
                        && Settings.AutoRefreshEnabled != value)
                    {
                        Settings.AutoRefreshEnabled = value;
                    }

                    UpdateAutoRefreshTimer();
                }
            }
        }

        public TimeSpan RefreshInterval
        {
            get { return _refreshInterval; }
            set
            {
                if (!IsSupportedRefreshInterval(value))
                {
                    throw new ArgumentOutOfRangeException(
                        "value",
                        "Refresh interval must be 5, 15, 30, or 60 minutes.");
                }

                if (SetProperty(ref _refreshInterval, value))
                {
                    int minutes = (int)value.TotalMinutes;
                    if (!_isApplyingSettings
                        && Settings.RefreshIntervalMinutes != minutes)
                    {
                        Settings.RefreshIntervalMinutes = minutes;
                    }

                    UpdateAutoRefreshTimer();
                }
            }
        }

        public ReadOnlyCollection<TimeSpan> RefreshIntervalOptions
        {
            get { return _refreshIntervalOptions; }
        }

        public ApplicationSettings Settings
        {
            get { return _settings; }
        }

        public ICommand RefreshCommand { get; private set; }

        public ICommand OpenSettingsCommand { get; private set; }

        public Task<bool> RefreshFromSettingsAsync()
        {
            return RefreshAsync("settings");
        }

        public SettingsViewModel CreateSettingsViewModel()
        {
            return new SettingsViewModel(_settingsService, Settings);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Tick -= AutoRefreshTimer_OnTick;
            Settings.PropertyChanged -= Settings_OnPropertyChanged;
            DisposeQuotaService(_quotaService);
        }

        private async Task<bool> RefreshAsync(string source)
        {
            if (IsRefreshing || _isDisposed)
            {
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            bool refreshSucceeded = false;
            int generation = _dataSourceGeneration;
            IQuotaService quotaService = _quotaService;
            IsRefreshing = true;
            Status = Settings.DataSourceType
                     == LocalDataSourceType.CodexSessionLogs
                ? "正在连接"
                : "正在同步";
            StatusType = SyncStatusType.Syncing;
            if (Settings.DataSourceType
                == LocalDataSourceType.CodexSessionLogs)
            {
                Status = "\u6b63\u5728\u8bfb\u53d6\u65e5\u5fd7";
            }
            LoggingService.LogRefreshStarted(source);

            try
            {
                QuotaSummary summary = await quotaService.GetQuotaAsync();
                if (generation != _dataSourceGeneration)
                {
                    return false;
                }

                if (summary == null)
                {
                    throw new InvalidOperationException("The quota service returned no data.");
                }

                QuotaSummary = summary;
                LastSyncTime = summary.LastSyncTime == DateTime.MinValue
                    ? DateTime.Now
                    : summary.LastSyncTime;
                Status = string.IsNullOrWhiteSpace(summary.Status)
                    ? "同步正常"
                    : summary.Status;
                StatusType = GetStatusType(Status);
                refreshSucceeded = StatusType == SyncStatusType.Normal;
                stopwatch.Stop();
                LoggingService.LogRefreshSucceeded(source, stopwatch.Elapsed);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                if (generation != _dataSourceGeneration)
                {
                    return false;
                }

                Status = "同步失败";
                StatusType = SyncStatusType.Failed;
                LoggingService.LogRefreshFailed(source, stopwatch.Elapsed, exception);
            }
            finally
            {
                IsRefreshing = false;
                if (_refreshPending && !_isDisposed)
                {
                    _refreshPending = false;
                    RefreshCommand.Execute("data-source");
                }
            }

            return refreshSucceeded;
        }

        private async void AutoRefreshTimer_OnTick(object sender, EventArgs e)
        {
            if (!IsRefreshing && !_isDisposed)
            {
                await RefreshAsync("automatic");
            }
        }

        private void UpdateAutoRefreshTimer()
        {
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Interval = RefreshInterval;

            if (AutoRefreshEnabled && !_isDisposed)
            {
                _autoRefreshTimer.Start();
            }
        }

        private void Settings_OnPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == "AutoRefreshEnabled"
                || e.PropertyName == "RefreshIntervalMinutes")
            {
                ApplyApplicationSettings();
            }

            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == "DataSourceType")
            {
                SwitchDataSource(Settings.DataSourceType);
            }
        }

        private void SwitchDataSource(LocalDataSourceType dataSourceType)
        {
            _dataSourceGeneration++;
            DisposeQuotaService(_quotaService);
            _quotaService = dataSourceType
                            == LocalDataSourceType.CodexSessionLogs
                ? (IQuotaService)new LocalCodexQuotaService(
                    new CodexFileScanner(),
                    new UsageParser())
                : new MockQuotaService();

            if (IsRefreshing)
            {
                _refreshPending = true;
            }
            else if (!_isDisposed)
            {
                RefreshCommand.Execute("data-source");
            }
        }

        private static string FormatResetTime(
            string label,
            DateTime? resetTime)
        {
            return resetTime.HasValue
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}：{1:yyyy-MM-dd HH:mm}",
                    label,
                    resetTime.Value)
                : string.Format("{0}：未提供", label);
        }

        private static SyncStatusType GetStatusType(string status)
        {
            if (string.Equals(
                status,
                LocalCodexQuotaService.DataNotFoundStatus,
                StringComparison.Ordinal))
            {
                return SyncStatusType.DataSourceNotFound;
            }

            if (string.Equals(
                status,
                LocalCodexQuotaService.ParseFailedStatus,
                StringComparison.Ordinal))
            {
                return SyncStatusType.Failed;
            }

            if (string.Equals(
                status,
                "额度已受限",
                StringComparison.Ordinal))
            {
                return SyncStatusType.DataSourceNotFound;
            }

            return SyncStatusType.Normal;
        }

        private void ApplyApplicationSettings()
        {
            _isApplyingSettings = true;
            try
            {
                RefreshInterval = TimeSpan.FromMinutes(
                    ApplicationSettings.NormalizeRefreshInterval(
                        Settings.RefreshIntervalMinutes));
                AutoRefreshEnabled = Settings.AutoRefreshEnabled;
            }
            finally
            {
                _isApplyingSettings = false;
            }
        }

        private void RaiseOpenSettingsRequested()
        {
            EventHandler handler = OpenSettingsRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static void DisposeQuotaService(IQuotaService quotaService)
        {
            var disposable = quotaService as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        private static bool IsSupportedRefreshInterval(TimeSpan interval)
        {
            return interval == TimeSpan.FromMinutes(5)
                   || interval == TimeSpan.FromMinutes(15)
                   || interval == TimeSpan.FromMinutes(30)
                   || interval == TimeSpan.FromMinutes(60);
        }

        private static bool IsQuotaAvailable(QuotaPeriod quota)
        {
            return quota != null
                   && !double.IsNaN(quota.Limit)
                   && !double.IsInfinity(quota.Limit)
                   && quota.Limit > 0;
        }
    }
}
