using System;
using System.Windows;
using CodexQuotaMonitor.Models;
using CodexQuotaMonitor.Services;
using CodexQuotaMonitor.ViewModels;
using CodexQuotaMonitor.Views;

namespace CodexQuotaMonitor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoggingService.LogApplicationStart();
            var settingsService = new SettingsService();
            ApplicationSettings settings = settingsService.Load();
            IQuotaService quotaService = settings.DataSourceType
                                         == LocalDataSourceType.CodexSessionLogs
                ? (IQuotaService)new LocalCodexQuotaService(
                    new CodexFileScanner(),
                    new UsageParser())
                : new MockQuotaService();

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    quotaService,
                    settingsService,
                    settings)
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var disposableViewModel = MainWindow == null
                ? null
                : MainWindow.DataContext as IDisposable;
            if (disposableViewModel != null)
            {
                disposableViewModel.Dispose();
            }

            base.OnExit(e);
        }
    }
}
