using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace CodexQuotaMonitor.Native
{
    public static class WindowHelper
    {
        private const int Windows11MinimumBuild = 22000;

        public static IntPtr GetWindowHandle(Window window)
        {
            if (window == null)
            {
                return IntPtr.Zero;
            }

            var interopHelper = new WindowInteropHelper(window);
            return interopHelper.Handle != IntPtr.Zero
                ? interopHelper.Handle
                : interopHelper.EnsureHandle();
        }

        public static Version GetWindowsVersion()
        {
            Version reportedVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return reportedVersion;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key == null)
                    {
                        return reportedVersion;
                    }

                    int major = ReadInteger(key, "CurrentMajorVersionNumber", reportedVersion.Major);
                    int minor = ReadInteger(key, "CurrentMinorVersionNumber", reportedVersion.Minor);
                    int build = ReadInteger(key, "CurrentBuildNumber", reportedVersion.Build);
                    int revision = ReadInteger(key, "UBR", 0);

                    return new Version(major, minor, Math.Max(build, 0), Math.Max(revision, 0));
                }
            }
            catch (Exception)
            {
                return reportedVersion;
            }
        }

        public static bool IsWindows11OrGreater()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            Version version = GetWindowsVersion();
            return version.Major > 10
                || (version.Major == 10 && version.Build >= Windows11MinimumBuild);
        }

        public static bool AreTransparencyEffectsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null)
                    {
                        return true;
                    }

                    return ReadInteger(key, "EnableTransparency", 1) != 0;
                }
            }
            catch (Exception)
            {
                // If the preference cannot be read, let the DWM call determine
                // whether Acrylic is available.
                return true;
            }
        }

        public static bool IsDwmSupported(IntPtr windowHandle)
        {
            Version version = GetWindowsVersion();
            if (windowHandle == IntPtr.Zero || version.Major < 6)
            {
                return false;
            }

            try
            {
                int isEnabled;
                int result = DwmApi.DwmGetWindowAttribute(
                    windowHandle,
                    DwmApi.DWMWA_NCRENDERING_ENABLED,
                    out isEnabled,
                    Marshal.SizeOf(typeof(int)));

                // Windows 8 and later always use desktop composition. A successful
                // query proves that DWM is available even when this borderless
                // window has not enabled non-client rendering yet.
                return result >= 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        private static int ReadInteger(RegistryKey key, string valueName, int fallback)
        {
            object value = key.GetValue(valueName);
            if (value == null)
            {
                return fallback;
            }

            if (value is int)
            {
                return (int)value;
            }

            int parsedValue;
            return int.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsedValue)
                ? parsedValue
                : fallback;
        }
    }
}
