using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CodexQuotaMonitor.Commands;
using CodexQuotaMonitor.Models;
using CodexQuotaMonitor.Services;

namespace CodexQuotaMonitor.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly SettingsService _settingsService;
        private readonly CodexFileScanner _scanner;
        private ApplicationSettings _originalSettings;
        private string _localLogStatus;
        private SyncStatusType _localLogStatusType;
        private bool _isScanningLocalLogs;
        private bool _isDisposed;

        public SettingsViewModel(
            SettingsService settingsService,
            ApplicationSettings applicationSettings)
        {
            if (settingsService == null)
            {
                throw new ArgumentNullException("settingsService");
            }

            if (applicationSettings == null)
            {
                throw new ArgumentNullException("applicationSettings");
            }

            _settingsService = settingsService;
            _scanner = new CodexFileScanner();
            ApplicationSettings = applicationSettings;
            _originalSettings = applicationSettings.Clone();
            _localLogStatus = "未扫描";
            _localLogStatusType = SyncStatusType.DataSourceNotFound;
            RefreshIntervalOptions = new List<int> { 5, 15, 30, 60 }.AsReadOnly();

            SaveCommand = new RelayCommand(parameter => Save());
            CancelCommand = new RelayCommand(parameter => Cancel());
            ScanLocalLogsCommand = new RelayCommand(
                async parameter => await ScanLocalLogsAsync(),
                parameter => !IsScanningLocalLogs);
            ApplicationSettings.PropertyChanged += ApplicationSettings_OnPropertyChanged;
        }

        public event Action<bool?> RequestClose;

        public ApplicationSettings ApplicationSettings { get; private set; }

        public ReadOnlyCollection<int> RefreshIntervalOptions { get; private set; }

        public ICommand SaveCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public ICommand ScanLocalLogsCommand { get; private set; }

        public string LocalLogStatus
        {
            get { return _localLogStatus; }
            private set { SetProperty(ref _localLogStatus, value); }
        }

        public SyncStatusType LocalLogStatusType
        {
            get { return _localLogStatusType; }
            private set { SetProperty(ref _localLogStatusType, value); }
        }

        public bool IsScanningLocalLogs
        {
            get { return _isScanningLocalLogs; }
            private set
            {
                if (SetProperty(ref _isScanningLocalLogs, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public void DiscardChanges()
        {
            ApplicationSettings.CopyFrom(_originalSettings);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            ApplicationSettings.PropertyChanged -= ApplicationSettings_OnPropertyChanged;
        }

        private async Task ScanLocalLogsAsync()
        {
            if (IsScanningLocalLogs || _isDisposed)
            {
                return;
            }

            IsScanningLocalLogs = true;
            LocalLogStatus = "正在扫描日志";
            LocalLogStatusType = SyncStatusType.Syncing;

            try
            {
                CodexDataLocation location = await Task.Run(
                    (Func<CodexDataLocation>)_scanner.Scan);
                if (_isDisposed)
                {
                    return;
                }

                if (location == null || !location.Found || location.Files.Count == 0)
                {
                    LocalLogStatus = LocalCodexQuotaService.DataNotFoundStatus;
                    LocalLogStatusType = SyncStatusType.DataSourceNotFound;
                    return;
                }

                LocalLogStatus = string.Format(
                    "找到 {0} 个会话日志",
                    location.Files.Count);
                LocalLogStatusType = SyncStatusType.Normal;
            }
            catch (Exception)
            {
                if (!_isDisposed)
                {
                    LocalLogStatus = LocalCodexQuotaService.ParseFailedStatus;
                    LocalLogStatusType = SyncStatusType.Failed;
                }
            }
            finally
            {
                if (!_isDisposed)
                {
                    IsScanningLocalLogs = false;
                }
            }
        }

        private void ApplicationSettings_OnPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if ((string.IsNullOrEmpty(e.PropertyName)
                 || e.PropertyName == "DataSourceType")
                && ApplicationSettings.DataSourceType
                == LocalDataSourceType.CodexSessionLogs)
            {
                LocalLogStatus = "未扫描";
                LocalLogStatusType = SyncStatusType.DataSourceNotFound;
            }
        }

        private void Save()
        {
            if (!_settingsService.Save(ApplicationSettings))
            {
                return;
            }

            _originalSettings = ApplicationSettings.Clone();
            RaiseRequestClose(true);
        }

        private void Cancel()
        {
            DiscardChanges();
            RaiseRequestClose(false);
        }

        private void RaiseRequestClose(bool? dialogResult)
        {
            Action<bool?> handler = RequestClose;
            if (handler != null)
            {
                handler(dialogResult);
            }
        }
    }
}
