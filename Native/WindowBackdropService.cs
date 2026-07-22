using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using CodexQuotaMonitor.Services;

namespace CodexQuotaMonitor.Native
{
    public sealed class BackdropResult
    {
        // Success specifically means that Desktop Acrylic was enabled.
        public bool Success { get; internal set; }

        public bool IsAcrylicEnabled { get; internal set; }

        public bool FallbackUsed { get; internal set; }

        public string ErrorMessage { get; internal set; }
    }

    public static class WindowBackdropService
    {
        private const int SystemBackdropMinimumBuild = 22621;
        public static BackdropResult EnableAcrylic(Window window)
        {
            return EnableAcrylic(window, 24.0, true);
        }

        public static BackdropResult EnableAcrylic(
            Window window,
            double cornerRadius)
        {
            return EnableAcrylic(window, cornerRadius, true);
        }

        public static BackdropResult EnableAcrylic(
            Window window,
            double cornerRadius,
            bool useDarkMode)
        {
            Version windowsVersion = WindowHelper.GetWindowsVersion();
            LoggingService.Info(string.Format("Current system version: {0}", windowsVersion));

            if (window == null)
            {
                return UseFallback(null, "Acrylic initialization failed: the window is null.");
            }

            string shadowError = TryConfigureSystemShadow(
                window,
                NormalizeCornerRadius(cornerRadius));
            if (!string.IsNullOrEmpty(shadowError))
            {
                LoggingService.Warning(shadowError);
            }

            try
            {
                IntPtr windowHandle = WindowHelper.GetWindowHandle(window);
                if (windowHandle == IntPtr.Zero)
                {
                    return UseFallback(window, "Acrylic initialization failed: no window handle is available.");
                }

                if (!WindowHelper.IsWindows11OrGreater())
                {
                    return UseFallback(window, "Desktop Acrylic requires Windows 11.");
                }

                if (!WindowHelper.IsDwmSupported(windowHandle))
                {
                    return UseFallback(window, "DWM is unavailable for this window.");
                }

                ApplyIndependentDwmAttributes(windowHandle, useDarkMode);

                if (!SupportsSystemBackdropType(windowsVersion))
                {
                    return UseFallback(
                        window,
                        string.Format(
                            "DWMWA_SYSTEMBACKDROP_TYPE requires Windows 11 Build {0} or later; current build is {1}.",
                            SystemBackdropMinimumBuild,
                            windowsVersion.Build));
                }

                string visualEffectsReason = GetVisualEffectsFallbackReason();
                if (!string.IsNullOrEmpty(visualEffectsReason))
                {
                    return UseFallback(window, visualEffectsReason);
                }

                MakeCompositionTargetTransparent(windowHandle);

                int backdropType = DwmApi.DWMSBT_TRANSIENTWINDOW;
                string backdropError;
                if (!TrySetAttribute(
                    windowHandle,
                    DwmApi.DWMWA_SYSTEMBACKDROP_TYPE,
                    backdropType,
                    "Desktop Acrylic backdrop",
                    out backdropError))
                {
                    return UseFallback(window, backdropError);
                }

                window.Background = Brushes.Transparent;

                LoggingService.Info("DWM initialization succeeded.");
                LoggingService.Info("Acrylic state: enabled; fallback used: false.");

                return new BackdropResult
                {
                    Success = true,
                    IsAcrylicEnabled = true,
                    FallbackUsed = false,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception exception)
            {
                LoggingService.Error("DWM initialization failed unexpectedly.", exception);
                return UseFallback(
                    window,
                    string.Format("DWM initialization failed: {0}", exception.Message));
            }
        }

        public static void UpdateImmersiveDarkMode(
            Window window,
            bool useDarkMode)
        {
            if (window == null)
            {
                return;
            }

            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            TrySetOptionalAttribute(
                windowHandle,
                DwmApi.DWMWA_USE_IMMERSIVE_DARK_MODE,
                useDarkMode ? 1 : 0,
                "immersive dark mode");
        }

        internal static bool SupportsSystemBackdropType(Version windowsVersion)
        {
            if (windowsVersion == null)
            {
                return false;
            }

            return windowsVersion.Major > 10
                || (windowsVersion.Major == 10
                    && windowsVersion.Build >= SystemBackdropMinimumBuild);
        }

        private static void ApplyIndependentDwmAttributes(
            IntPtr windowHandle,
            bool useDarkMode)
        {
            TrySetOptionalAttribute(
                windowHandle,
                DwmApi.DWMWA_USE_IMMERSIVE_DARK_MODE,
                useDarkMode ? 1 : 0,
                "immersive dark mode");

            TrySetOptionalAttribute(
                windowHandle,
                DwmApi.DWMWA_WINDOW_CORNER_PREFERENCE,
                DwmApi.DWMWCP_ROUND,
                "rounded corners");

            TrySetOptionalAttribute(
                windowHandle,
                DwmApi.DWMWA_BORDER_COLOR,
                DwmApi.DWMWA_COLOR_NONE,
                "borderless glass edge");

            TrySetOptionalAttribute(
                windowHandle,
                DwmApi.DWMWA_NCRENDERING_POLICY,
                DwmApi.DWMNCRP_ENABLED,
                "DWM non-client rendering");
        }

        private static void TrySetOptionalAttribute(
            IntPtr windowHandle,
            int attribute,
            int value,
            string description)
        {
            string errorMessage;
            if (!TrySetAttribute(
                windowHandle,
                attribute,
                value,
                description,
                out errorMessage))
            {
                LoggingService.Warning(errorMessage);
            }
        }

        private static bool TrySetAttribute(
            IntPtr windowHandle,
            int attribute,
            int value,
            string description,
            out string errorMessage)
        {
            try
            {
                int result = DwmApi.DwmSetWindowAttribute(
                    windowHandle,
                    attribute,
                    ref value,
                    Marshal.SizeOf(typeof(int)));

                if (result < 0)
                {
                    errorMessage = string.Format(
                        "DWM could not apply {0}. HRESULT: 0x{1:X8}",
                        description,
                        result);
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }
            catch (DllNotFoundException exception)
            {
                errorMessage = string.Format("DWM is unavailable while applying {0}: {1}", description, exception.Message);
                return false;
            }
            catch (EntryPointNotFoundException exception)
            {
                errorMessage = string.Format("The DWM API is unavailable while applying {0}: {1}", description, exception.Message);
                return false;
            }
            catch (ExternalException exception)
            {
                errorMessage = string.Format("DWM rejected {0}: {1}", description, exception.Message);
                return false;
            }
            catch (Exception exception)
            {
                errorMessage = string.Format("DWM failed while applying {0}: {1}", description, exception.Message);
                return false;
            }
        }

        private static string GetVisualEffectsFallbackReason()
        {
            if (SystemParameters.HighContrast)
            {
                return "Acrylic was disabled because Windows high-contrast mode is active.";
            }

            if (SystemParameters.IsRemoteSession)
            {
                return "Acrylic was disabled because the application is running in a remote session.";
            }

            if (!WindowHelper.AreTransparencyEffectsEnabled())
            {
                return "Acrylic was disabled because Windows transparency effects are turned off.";
            }

            return string.Empty;
        }

        private static BackdropResult UseFallback(Window window, string reason)
        {
            string fallbackError;
            bool fallbackApplied = TryApplyFallback(window, out fallbackError);
            string resultMessage = fallbackApplied || string.IsNullOrEmpty(fallbackError)
                ? reason
                : string.Format("{0} Fallback error: {1}", reason, fallbackError);

            LoggingService.Warning(string.Format("DWM initialization failed or was skipped: {0}", reason));
            LoggingService.Info(string.Format(
                "Acrylic state: disabled; fallback used: {0}; fallback reason: {1}",
                fallbackApplied,
                resultMessage));

            return new BackdropResult
            {
                Success = false,
                IsAcrylicEnabled = false,
                FallbackUsed = fallbackApplied,
                ErrorMessage = resultMessage
            };
        }

        private static bool TryApplyFallback(Window window, out string errorMessage)
        {
            if (window == null)
            {
                errorMessage = "The fallback background could not be applied because the window is null.";
                return false;
            }

            try
            {
                // The XAML backdrop layer owns the fallback color. Keeping the
                // HWND transparent here prevents a failed initialization from
                // leaving a dark brush behind when Acrylic later succeeds.
                window.Background = Brushes.Transparent;
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                LoggingService.Error("The fallback glass background could not be applied.", exception);
                return false;
            }
        }

        private static void MakeCompositionTargetTransparent(IntPtr windowHandle)
        {
            HwndSource source = HwndSource.FromHwnd(windowHandle);
            if (source != null && source.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }

        private static string TryConfigureSystemShadow(
            Window window,
            double cornerRadius)
        {
            try
            {
                if (WindowChrome.GetWindowChrome(window) != null)
                {
                    return string.Empty;
                }

                var chrome = new WindowChrome
                {
                    CaptionHeight = 0,
                    // Windows 11 already clips the HWND using the native DWM
                    // corner. Applying a second WindowChrome radius produces
                    // the visible inner curve seen in dark mode.
                    CornerRadius = WindowHelper.IsWindows11OrGreater()
                        ? new CornerRadius(0)
                        : new CornerRadius(cornerRadius),
                    // A full-sheet glass frame is required for WPF's client
                    // area to reveal DWMWA_SYSTEMBACKDROP_TYPE. A zero frame
                    // leaves the transparent composition target backed by black.
                    GlassFrameThickness = new Thickness(-1),
                    ResizeBorderThickness = new Thickness(0),
                    UseAeroCaptionButtons = false
                };

                WindowChrome.SetWindowChrome(window, chrome);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return string.Format("The system window shadow could not be configured: {0}", exception.Message);
            }
        }

        private static double NormalizeCornerRadius(double cornerRadius)
        {
            if (double.IsNaN(cornerRadius)
                || double.IsInfinity(cornerRadius)
                || cornerRadius < 0)
            {
                return 24.0;
            }

            return cornerRadius;
        }

    }
}
