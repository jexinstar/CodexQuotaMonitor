using System;
using System.IO;
using System.Text;

namespace CodexQuotaMonitor.Services
{
    public static class LoggingService
    {
        private static readonly object SyncRoot = new object();
        private static readonly Encoding LogEncoding = new UTF8Encoding(false);

        public static string LogDirectoryPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "CodexQuotaMonitor",
                    "Logs");
            }
        }

        public static void LogApplicationStart()
        {
            Info(string.Format(
                "Application started. StartupTime={0:O}.",
                DateTimeOffset.Now));
        }

        public static void LogRefreshStarted(string source)
        {
            Info(string.Format(
                "Quota refresh started. Source={0}.",
                NormalizeRefreshSource(source)));
        }

        public static void LogRefreshSucceeded(string source, TimeSpan elapsed)
        {
            Info(string.Format(
                "Quota refresh succeeded. Source={0}; DurationMs={1}.",
                NormalizeRefreshSource(source),
                Milliseconds(elapsed)));
        }

        public static void LogRefreshFailed(
            string source,
            TimeSpan elapsed,
            Exception exception)
        {
            Error(string.Format(
                "Quota refresh failed. Source={0}; DurationMs={1}; ExceptionType={2}; HResult=0x{3:X8}.",
                NormalizeRefreshSource(source),
                Milliseconds(elapsed),
                ExceptionType(exception),
                exception == null ? 0 : exception.HResult));
        }

        public static void LogSettingsLoaded()
        {
            Info("Application settings loaded successfully.");
        }

        public static void LogSettingsSaved()
        {
            Info("Application settings saved successfully.");
        }

        public static void LogSettingsFailed(string operation, Exception exception)
        {
            Error(string.Format(
                "Application settings {0} failed. ExceptionType={1}; HResult=0x{2:X8}.",
                operation == "save" ? "save" : "load",
                ExceptionType(exception),
                exception == null ? 0 : exception.HResult));
        }

        public static void LogCodexScanStarted()
        {
            Info("Local session-log scan started.");
        }

        public static void LogCodexDirectoryFound(string sourceName)
        {
            Info(string.Format(
                "Local session-log directory found. Source={0}.",
                sourceName == "sessions" ? "sessions" : "custom"));
        }

        public static void LogCodexScanCompleted(
            bool found,
            int fileCount,
            TimeSpan elapsed)
        {
            Info(string.Format(
                "Local session-log scan completed. Found={0}; FileCount={1}; DurationMs={2}.",
                found,
                Math.Max(0, fileCount),
                Milliseconds(elapsed)));
        }

        public static void LogCodexScanFailed(TimeSpan elapsed, Exception exception)
        {
            Error(string.Format(
                "Local session-log scan failed. DurationMs={0}; ExceptionType={1}; HResult=0x{2:X8}.",
                Milliseconds(elapsed),
                ExceptionType(exception),
                exception == null ? 0 : exception.HResult));
        }

        public static void LogCodexParseSucceeded(TimeSpan elapsed)
        {
            Info(string.Format(
                "Local session-log parse succeeded. DurationMs={0}.",
                Milliseconds(elapsed)));
        }

        public static void LogCodexParseFailed(
            TimeSpan elapsed,
            string reason,
            Exception exception)
        {
            Error(string.Format(
                "Local session-log parse failed. Reason={0}; DurationMs={1}; ExceptionType={2}; HResult=0x{3:X8}.",
                reason == "parser-error" ? "parser-error" : "unsupported-format",
                Milliseconds(elapsed),
                ExceptionType(exception),
                exception == null ? 0 : exception.HResult));
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception exception)
        {
            Error(string.Format(
                "{0} ExceptionType={1}; HResult=0x{2:X8}.",
                message ?? string.Empty,
                ExceptionType(exception),
                exception == null ? 0 : exception.HResult));
        }

        private static long Milliseconds(TimeSpan elapsed)
        {
            return Math.Max(0, (long)elapsed.TotalMilliseconds);
        }

        private static string ExceptionType(Exception exception)
        {
            return exception == null ? "None" : exception.GetType().FullName;
        }

        private static string NormalizeRefreshSource(string source)
        {
            switch (source)
            {
                case "startup":
                case "manual":
                case "automatic":
                case "data-source":
                    return source;
                default:
                    return "unknown";
            }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectoryPath);
                    string logPath = Path.Combine(
                        LogDirectoryPath,
                        string.Format(
                            "CodexQuotaMonitor-{0:yyyyMMdd}.log",
                            DateTime.Now));
                    string entry = string.Format(
                        "{0:O} [{1}] {2}{3}",
                        DateTimeOffset.Now,
                        level,
                        message ?? string.Empty,
                        Environment.NewLine);
                    File.AppendAllText(logPath, entry, LogEncoding);
                }
            }
            catch (Exception)
            {
                // Observability must never affect quota display.
            }
        }
    }
}
