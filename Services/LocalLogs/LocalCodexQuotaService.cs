using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public sealed class LocalCodexQuotaService : IQuotaService
    {
        public const string DataNotFoundStatus = "本地数据未找到";
        public const string ParseFailedStatus = "本地数据解析失败";

        private readonly CodexFileScanner _scanner;
        private readonly UsageParser _parser;

        public LocalCodexQuotaService(
            CodexFileScanner scanner,
            UsageParser parser)
        {
            if (scanner == null)
            {
                throw new ArgumentNullException("scanner");
            }

            if (parser == null)
            {
                throw new ArgumentNullException("parser");
            }

            _scanner = scanner;
            _parser = parser;
        }

        public Task<QuotaSummary> GetQuotaAsync()
        {
            return Task.Run((Func<QuotaSummary>)LoadLocalQuota);
        }

        private QuotaSummary LoadLocalQuota()
        {
            CodexDataLocation location = _scanner.Scan();
            if (location == null
                || !location.Found
                || location.Files.Count == 0)
            {
                return CreateUnavailableSummary(DataNotFoundStatus);
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                QuotaSummary summary = _parser.Parse(location);
                stopwatch.Stop();

                if (summary == null)
                {
                    LoggingService.LogCodexParseFailed(
                        stopwatch.Elapsed,
                        "unsupported-format",
                        null);
                    return CreateUnavailableSummary(ParseFailedStatus);
                }

                EnsurePeriods(summary);
                summary.Status = string.IsNullOrWhiteSpace(summary.Status)
                    ? "同步正常"
                    : summary.Status;
                summary.LastSyncTime = summary.LastSyncTime == DateTime.MinValue
                    ? DateTime.Now
                    : summary.LastSyncTime;
                LoggingService.LogCodexParseSucceeded(stopwatch.Elapsed);
                return summary;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                LoggingService.LogCodexParseFailed(
                    stopwatch.Elapsed,
                    "parser-error",
                    exception);
                return CreateUnavailableSummary(ParseFailedStatus);
            }
        }

        private static QuotaSummary CreateUnavailableSummary(string status)
        {
            return new QuotaSummary
            {
                TodayQuota = CreateEmptyPeriod("今日额度", "AccentBlue"),
                WeekQuota = CreateEmptyPeriod("本周额度", "AccentGreen"),
                MonthQuota = CreateEmptyPeriod("本月额度", "AccentPurple"),
                RemainingQuota = CreateEmptyPeriod("剩余额度", "AccentBlue"),
                Status = status,
                LastSyncTime = DateTime.Now
            };
        }

        private static void EnsurePeriods(QuotaSummary summary)
        {
            summary.TodayQuota = summary.TodayQuota
                                 ?? CreateEmptyPeriod("今日额度", "AccentBlue");
            summary.WeekQuota = summary.WeekQuota
                                ?? CreateEmptyPeriod("本周额度", "AccentGreen");
            summary.MonthQuota = summary.MonthQuota
                                 ?? CreateEmptyPeriod("本月额度", "AccentPurple");
            summary.RemainingQuota = summary.RemainingQuota
                                     ?? CreateEmptyPeriod("剩余额度", "AccentBlue");
        }

        private static QuotaPeriod CreateEmptyPeriod(
            string name,
            string accentColor)
        {
            return new QuotaPeriod
            {
                Name = name,
                Used = 0,
                Limit = 0,
                Unit = "tokens",
                AccentColor = accentColor
            };
        }
    }
}
