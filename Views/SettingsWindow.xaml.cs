using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using CodexQuotaMonitor.Native;
using CodexQuotaMonitor.ViewModels;

namespace CodexQuotaMonitor.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel _viewModel;
        private bool _closeRequested;

        public SettingsWindow()
        {
            InitializeComponent();
            SourceInitialized += SettingsWindow_OnSourceInitialized;
            DataContextChanged += SettingsWindow_OnDataContextChanged;
        }

        public BackdropResult BackdropResult { get; private set; }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_closeRequested && _viewModel != null)
            {
                _viewModel.DiscardChanges();
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            SettingsViewModel viewModel = _viewModel;
            AttachViewModel(null);
            if (viewModel != null)
            {
                viewModel.Dispose();
            }

            base.OnClosed(e);
        }

        private void SettingsWindow_OnSourceInitialized(
            object sender,
            EventArgs e)
        {
            bool useDarkMode = _viewModel == null
                               || _viewModel.ApplicationSettings.DarkMode;
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

        private void SettingsWindow_OnDataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(e.NewValue as SettingsViewModel);
        }

        private void AttachViewModel(SettingsViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= ViewModel_OnRequestClose;
                _viewModel.ApplicationSettings.PropertyChanged -=
                    ApplicationSettings_OnPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.RequestClose += ViewModel_OnRequestClose;
                _viewModel.ApplicationSettings.PropertyChanged +=
                    ApplicationSettings_OnPropertyChanged;
                ApplyBackgroundOpacity();
            }
        }

        private void ApplicationSettings_OnPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (_viewModel != null
                && (string.IsNullOrEmpty(e.PropertyName)
                    || e.PropertyName == "DarkMode"))
            {
                WindowBackdropService.UpdateImmersiveDarkMode(
                    this,
                    _viewModel.ApplicationSettings.DarkMode);
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
                    _viewModel.ApplicationSettings.BackgroundOpacity;
            }
        }

        private void ViewModel_OnRequestClose(bool? dialogResult)
        {
            _closeRequested = true;

            try
            {
                DialogResult = dialogResult;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
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
                // The mouse button may be released before DragMove starts.
            }
        }

        private void ExitApplicationButton_OnClick(
            object sender,
            RoutedEventArgs e)
        {
            Application application = Application.Current;
            if (application != null)
            {
                application.Shutdown();
            }
        }

        private async void UpdateQuotaButton_OnClick(
            object sender,
            RoutedEventArgs e)
        {
            MainWindow mainWindow = Owner as MainWindow;
            MainViewModel mainViewModel = mainWindow == null
                ? null
                : mainWindow.DataContext as MainViewModel;

            if (mainViewModel == null)
            {
                SetQuotaUpdateStatus("更新失败", "ErrorColor");
                return;
            }

            if (mainViewModel.IsRefreshing)
            {
                SetQuotaUpdateStatus("正在更新", "AccentBlue");
                return;
            }

            UpdateQuotaButton.IsEnabled = false;
            SetQuotaUpdateStatus("正在更新", "AccentBlue");

            try
            {
                bool succeeded = await mainViewModel.RefreshFromSettingsAsync();
                SetQuotaUpdateStatus(
                    succeeded ? "更新成功" : "更新失败",
                    succeeded ? "AccentGreen" : "ErrorColor");
            }
            catch (Exception)
            {
                SetQuotaUpdateStatus("更新失败", "ErrorColor");
            }
            finally
            {
                UpdateQuotaButton.IsEnabled = true;
            }
        }

        private void SetQuotaUpdateStatus(string message, string colorResourceKey)
        {
            QuotaUpdateStatusText.Text = message;
            QuotaUpdateStatusText.SetResourceReference(
                TextBlock.ForegroundProperty,
                colorResourceKey);
            QuotaUpdateStatusDot.SetResourceReference(
                Shape.FillProperty,
                colorResourceKey);
        }
    }
}
