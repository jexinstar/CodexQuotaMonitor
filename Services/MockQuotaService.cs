using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public sealed class MockQuotaService : IQuotaService
    {
        public async Task<QuotaSummary> GetQuotaAsync()
        {
            await Task.Delay(650);

            var summary = new QuotaSummary
            {
                TodayQuota = CreatePeriod("今日额度", 6800, 10000, "tokens", "AccentBlue"),
                WeekQuota = CreatePeriod("本周额度", 29400, 70000, "tokens", "AccentGreen"),
                MonthQuota = CreatePeriod("本月额度", 162000, 200000, "tokens", "AccentPurple"),
                RemainingQuota = CreatePeriod("剩余额度", 38000, 200000, "tokens", "AccentBlue"),
                Trend = new ObservableCollection<UsageTrendPoint>
                {
                    CreateTrendPoint(2024, 5, 14, 2000),
                    CreateTrendPoint(2024, 5, 15, 3800),
                    CreateTrendPoint(2024, 5, 16, 4000),
                    CreateTrendPoint(2024, 5, 17, 5200),
                    CreateTrendPoint(2024, 5, 18, 7200),
                    CreateTrendPoint(2024, 5, 19, 4800),
                    CreateTrendPoint(2024, 5, 20, 3100)
                },
                Status = "同步正常",
                LastSyncTime = DateTime.Now
            };

            return summary;
        }

        private static QuotaPeriod CreatePeriod(
            string name,
            double used,
            double limit,
            string unit,
            string accentColor)
        {
            return new QuotaPeriod
            {
                Name = name,
                Used = used,
                Limit = limit,
                Unit = unit,
                AccentColor = accentColor
            };
        }

        private static UsageTrendPoint CreateTrendPoint(
            int year,
            int month,
            int day,
            double value)
        {
            return new UsageTrendPoint
            {
                Date = new DateTime(year, month, day),
                Value = value
            };
        }
    }
}
