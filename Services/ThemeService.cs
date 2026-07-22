using System;
using System.Windows;

namespace CodexQuotaMonitor.Services
{
    public static class ThemeService
    {
        private const string DarkThemePath =
            "/CodexQuotaMonitor;component/Themes/DarkTheme.xaml";
        private const string LightThemePath =
            "/CodexQuotaMonitor;component/Themes/LightTheme.xaml";

        public static bool ApplyTheme(bool darkMode)
        {
            try
            {
                Application application = Application.Current;
                if (application == null)
                {
                    return false;
                }

                string targetPath = darkMode ? DarkThemePath : LightThemePath;
                ResourceDictionary currentTheme = FindCurrentTheme(application);
                if (currentTheme != null
                    && currentTheme.Source != null
                    && currentTheme.Source.OriginalString.EndsWith(
                        darkMode ? "DarkTheme.xaml" : "LightTheme.xaml",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(targetPath, UriKind.Relative)
                };

                if (currentTheme == null)
                {
                    application.Resources.MergedDictionaries.Insert(0, newTheme);
                }
                else
                {
                    int index = application.Resources.MergedDictionaries
                        .IndexOf(currentTheme);
                    application.Resources.MergedDictionaries[index] = newTheme;
                }

                LoggingService.Info(string.Format(
                    "Application theme changed successfully. Mode={0}.",
                    darkMode ? "dark" : "light"));
                return true;
            }
            catch (Exception exception)
            {
                LoggingService.Warning(string.Format(
                    "Application theme change failed. ExceptionType={0}; HResult=0x{1:X8}.",
                    exception.GetType().FullName,
                    exception.HResult));
                return false;
            }
        }

        private static ResourceDictionary FindCurrentTheme(
            Application application)
        {
            foreach (ResourceDictionary dictionary
                     in application.Resources.MergedDictionaries)
            {
                if (dictionary.Source == null)
                {
                    continue;
                }

                string source = dictionary.Source.OriginalString;
                if (source.EndsWith(
                        "DarkTheme.xaml",
                        StringComparison.OrdinalIgnoreCase)
                    || source.EndsWith(
                        "LightTheme.xaml",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return dictionary;
                }
            }

            return null;
        }
    }
}
