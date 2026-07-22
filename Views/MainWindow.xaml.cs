using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CodexQuotaMonitor.Native;
using CodexQuotaMonitor.Services;
using CodexQuotaMonitor.ViewModels;

namespace CodexQuotaMonitor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private SettingsWindow _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_OnSourceInitialized;
            DataContextChanged += MainWindow_OnDataContextChanged;
            Closed += MainWindow_OnClosed;
        }

        public BackdropResult BackdropResult { get; private set; }

        private void MainWindow_OnSourceInitialized(object sender, EventArgs e)
        {
            bool useDarkMode = _viewModel == null
                               || _viewModel.Settings.DarkMode;
            BackdropResult = WindowBackdropService.EnableAcrylic(
                this,
                18.0,
                useDarkMode);
            UpdateBackdropLayers(BackdropResult);
        }

        private void UpdateBackdropLayers(BackdropResult result)
        {
            bool useNativeBackdrop = result != null
                                     && result.IsAcrylicEnabled
                                     && !result.FallbackUsed;

            NativeBackdropTint.Visibility = useNativeBackdrop
                ? Visibility.Visible
                : Visibility.Collapsed;
            FallbackBackdrop.Visibility = useNativeBackdrop
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void MainWindow_OnDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as MainViewModel);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            AttachViewModel(null);
        }

        private void AttachViewModel(MainViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.OpenSettingsRequested -= ViewModel_OnOpenSettingsRequested;
                _viewModel.Settings.PropertyChanged -= Settings_OnPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel == null)
            {
                return;
            }

            _viewModel.OpenSettingsRequested += ViewModel_OnOpenSettingsRequested;
            _viewModel.Settings.PropertyChanged += Settings_OnPropertyChanged;
            ThemeService.ApplyTheme(_viewModel.Settings.DarkMode);
            ApplyBackgroundOpacity();
        }

        private void Settings_OnPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (_viewModel != null
                && (string.IsNullOrEmpty(e.PropertyName)
                    || e.PropertyName == "DarkMode"))
            {
                ThemeService.ApplyTheme(_viewModel.Settings.DarkMode);
                WindowBackdropService.UpdateImmersiveDarkMode(
                    this,
                    _viewModel.Settings.DarkMode);
            }

            if (_viewModel != null
                && (string.IsNullOrEmpty(e.PropertyName)
                    || e.PropertyName == "BackgroundOpacity"))
            {
                ApplyBackgroundOpacity();
            }
        }

        private void ApplyBackgroundOpacity()
        {
            if (_viewModel != null)
            {
                NativeBackdropTint.Opacity =
                    _viewModel.Settings.BackgroundOpacity;
            }
        }

        private void ViewModel_OnOpenSettingsRequested(object sender, EventArgs e)
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow
            {
                Owner = this,
                DataContext = _viewModel.CreateSettingsViewModel()
            };
            _settingsWindow.Closed += SettingsWindow_OnClosed;
            _settingsWindow.ShowDialog();
        }

        private void SettingsWindow_OnClosed(object sender, EventArgs e)
        {
            if (_settingsWindow == null)
            {
                return;
            }

            _settingsWindow.Closed -= SettingsWindow_OnClosed;
            _settingsWindow = null;
        }

        private void TitleBar_OnMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // The button can be released between the event and DragMove.
            }
        }
    }
}
