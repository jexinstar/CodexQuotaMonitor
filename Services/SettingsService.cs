using System;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public sealed class SettingsService
    {
        private readonly DataContractJsonSerializer _serializer =
            new DataContractJsonSerializer(typeof(ApplicationSettings));
        private readonly bool _managesStartupRegistration;

        public SettingsService(string settingsFilePath = null)
        {
            _managesStartupRegistration =
                string.IsNullOrWhiteSpace(settingsFilePath);
            SettingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
                ? Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "CodexQuotaMonitor",
                    "settings.json")
                : Path.GetFullPath(settingsFilePath);
        }

        public string SettingsFilePath { get; private set; }

        public ApplicationSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    var defaultSettings = new ApplicationSettings();
                    Save(defaultSettings);
                    LoggingService.LogSettingsLoaded();
                    return defaultSettings;
                }

                using (var stream = new FileStream(
                    SettingsFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    var settings =
                        _serializer.ReadObject(stream) as ApplicationSettings;
                    if (settings == null)
                    {
                        throw new InvalidDataException(
                            "The settings file did not contain application settings.");
                    }

                    settings.RefreshIntervalMinutes =
                        ApplicationSettings.NormalizeRefreshInterval(
                            settings.RefreshIntervalMinutes);
                    LoggingService.LogSettingsLoaded();
                    return settings;
                }
            }
            catch (Exception exception)
            {
                LoggingService.LogSettingsFailed("load", exception);
                return new ApplicationSettings();
            }
        }

        public bool Save(ApplicationSettings settings)
        {
            if (settings == null)
            {
                LoggingService.LogSettingsFailed(
                    "save",
                    new ArgumentNullException("settings"));
                return false;
            }

            string temporaryPath = SettingsFilePath + ".tmp";

            try
            {
                string directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    _serializer.WriteObject(stream, settings);
                    stream.Flush();
                }

                if (File.Exists(SettingsFilePath))
                {
                    ReplaceSettingsFile(temporaryPath);
                }
                else
                {
                    File.Move(temporaryPath, SettingsFilePath);
                }

                if (_managesStartupRegistration)
                {
                    TryApplyStartupRegistration(settings.StartWithWindows);
                }

                LoggingService.LogSettingsSaved();
                return true;
            }
            catch (Exception exception)
            {
                LoggingService.LogSettingsFailed("save", exception);
                TryDeleteTemporaryFile(temporaryPath);
                return false;
            }
        }

        private void ReplaceSettingsFile(string temporaryPath)
        {
            try
            {
                File.Replace(temporaryPath, SettingsFilePath, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temporaryPath, SettingsFilePath, true);
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                File.Copy(temporaryPath, SettingsFilePath, true);
                File.Delete(temporaryPath);
            }
        }

        private static void TryDeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // A failed cleanup must never terminate the application.
            }
        }

        private static void TryApplyStartupRegistration(bool enabled)
        {
            try
            {
                const string runKeyPath =
                    @"Software\Microsoft\Windows\CurrentVersion\Run";
                const string valueName = "CodexQuotaMonitor";

                using (RegistryKey runKey =
                    Registry.CurrentUser.OpenSubKey(runKeyPath, true))
                {
                    if (runKey == null)
                    {
                        throw new InvalidOperationException(
                            "The current-user startup key is unavailable.");
                    }

                    if (enabled)
                    {
                        string executablePath =
                            typeof(SettingsService).Assembly.Location;
                        runKey.SetValue(
                            valueName,
                            string.Format("\"{0}\"", executablePath),
                            RegistryValueKind.String);
                    }
                    else
                    {
                        runKey.DeleteValue(valueName, false);
                    }
                }
            }
            catch (Exception exception)
            {
                LoggingService.Warning(string.Format(
                    "Startup registration update failed. ExceptionType={0}; HResult=0x{1:X8}.",
                    exception.GetType().FullName,
                    exception.HResult));
            }
        }
    }
}
