using System;
using System.Collections.ObjectModel;

namespace CodexQuotaMonitor.Models
{
    public sealed class QuotaSummary
    {
        public QuotaSummary()
        {
            Trend = new ObservableCollection<UsageTrendPoint>();
        }

        public QuotaPeriod TodayQuota { get; set; }

        public QuotaPeriod WeekQuota { get; set; }

        public QuotaPeriod MonthQuota { get; set; }

        public QuotaPeriod RemainingQuota { get; set; }

        public CodexAccountInfo Account { get; set; }

        public bool? RequiresOpenAiAuth { get; set; }

        public DateTime? FiveHourResetTime { get; set; }

        public DateTime? SevenDayResetTime { get; set; }

        public ObservableCollection<UsageTrendPoint> Trend { get; set; }

        public string Status { get; set; }

        public DateTime LastSyncTime { get; set; }
    }
}
