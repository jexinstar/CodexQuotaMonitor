using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    // Maps the rate_limits snapshot already extracted from a local session log
    // into the UI model. No RPC or remote response parsing belongs here.
    public sealed class CodexQuotaResponseParser
    {
        private const long FiveHourWindowMinutes = 5 * 60;
        private const long SevenDayWindowMinutes = 7 * 24 * 60;

        public QuotaSummary ToQuotaSummary(CodexQuotaResponse response)
        {
            CodexRateLimitSnapshot snapshot = response == null
                ? null
                : response.RateLimits;
            if (snapshot == null)
            {
                return null;
            }

            IList<CodexRateLimitWindow> windows = GetWindows(snapshot);
            if (windows.Count == 0)
            {
                return null;
            }

            CodexRateLimitWindow fiveHour = FindWindow(
                windows,
                FiveHourWindowMinutes);
            CodexRateLimitWindow sevenDay = FindWindow(
                windows,
                SevenDayWindowMinutes);
            IList<CodexRateLimitWindow> fallbacks = windows
                .Where(item => !HasDuration(item, FiveHourWindowMinutes)
                               && !HasDuration(item, SevenDayWindowMinutes))
                .OrderBy(item => item.WindowMinutes ?? long.MaxValue)
                .ToList();

            if (fiveHour == null && fallbacks.Count > 0)
            {
                fiveHour = fallbacks[0];
            }

            if (sevenDay == null)
            {
                sevenDay = fallbacks.LastOrDefault(
                    item => !ReferenceEquals(item, fiveHour));
            }

            double maximumUsed = windows.Max(item => NormalizePercent(item.UsedPercent));
            return new QuotaSummary
            {
                TodayQuota = CreatePeriod(
                    fiveHour,
                    GetWindowName(
                        fiveHour,
                        FiveHourWindowMinutes,
                        "\u0035\u5c0f\u65f6\u989d\u5ea6",
                        "\u77ed\u5468\u671f\u989d\u5ea6"),
                    "AccentBlue"),
                WeekQuota = CreatePeriod(
                    sevenDay,
                    GetWindowName(
                        sevenDay,
                        SevenDayWindowMinutes,
                        "\u0037\u65e5\u989d\u5ea6",
                        "\u957f\u5468\u671f\u989d\u5ea6"),
                    "AccentPurple"),
                MonthQuota = CreateEmptyPeriod(
                    "\u672a\u63d0\u4f9b",
                    "AccentGreen"),
                RemainingQuota = new QuotaPeriod
                {
                    Name = "\u6700\u8fd1\u7a97\u53e3\u5269\u4f59",
                    Used = Math.Max(0.0, 100.0 - maximumUsed),
                    Limit = 100.0,
                    Unit = "%",
                    AccentColor = "AccentBlue"
                },
                FiveHourResetTime = ToLocalTime(
                    fiveHour == null ? null : fiveHour.ResetsAt),
                SevenDayResetTime = ToLocalTime(
                    sevenDay == null ? null : sevenDay.ResetsAt),
                Trend = new ObservableCollection<UsageTrendPoint>(),
                Status = string.IsNullOrWhiteSpace(snapshot.RateLimitReachedType)
                    ? "\u540c\u6b65\u6b63\u5e38"
                    : "\u989d\u5ea6\u5df2\u53d7\u9650",
                LastSyncTime = DateTime.Now
            };
        }

        private static IList<CodexRateLimitWindow> GetWindows(
            CodexRateLimitSnapshot snapshot)
        {
            var windows = new List<CodexRateLimitWindow>();
            if (snapshot.Primary != null)
            {
                windows.Add(snapshot.Primary);
            }

            if (snapshot.Secondary != null)
            {
                windows.Add(snapshot.Secondary);
            }

            return windows;
        }

        private static CodexRateLimitWindow FindWindow(
            IEnumerable<CodexRateLimitWindow> windows,
            long expectedMinutes)
        {
            return windows.FirstOrDefault(item => HasDuration(item, expectedMinutes));
        }

        private static bool HasDuration(
            CodexRateLimitWindow window,
            long expectedMinutes)
        {
            return window != null
                   && window.WindowMinutes.HasValue
                   && window.WindowMinutes.Value == expectedMinutes;
        }

        private static string GetWindowName(
            CodexRateLimitWindow window,
            long expectedMinutes,
            string expectedName,
            string fallbackName)
        {
            if (window == null)
            {
                return expectedName + "\uff08\u672a\u63d0\u4f9b\uff09";
            }

            return HasDuration(window, expectedMinutes)
                ? expectedName
                : fallbackName;
        }

        private static QuotaPeriod CreatePeriod(
            CodexRateLimitWindow window,
            string name,
            string accentColor)
        {
            if (window == null)
            {
                return CreateEmptyPeriod(name, accentColor);
            }

            return new QuotaPeriod
            {
                Name = name,
                Used = NormalizePercent(window.UsedPercent),
                Limit = 100.0,
                Unit = "%",
                AccentColor = accentColor,
                WindowDurationMinutes = window.WindowMinutes,
                ResetAt = ToLocalTime(window.ResetsAt)
            };
        }

        private static QuotaPeriod CreateEmptyPeriod(
            string name,
            string accentColor)
        {
            return new QuotaPeriod
            {
                Name = name,
                Used = 0,
                Limit = 100,
                Unit = "%",
                AccentColor = accentColor
            };
        }

        private static DateTime? ToLocalTime(long? unixSeconds)
        {
            if (!unixSeconds.HasValue || unixSeconds.Value <= 0)
            {
                return null;
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(
                    unixSeconds.Value).LocalDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static double NormalizePercent(double? value)
        {
            if (!value.HasValue
                || double.IsNaN(value.Value)
                || double.IsInfinity(value.Value))
            {
                return 0;
            }

            return Math.Max(0.0, Math.Min(100.0, value.Value));
        }
    }
}
