using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public sealed class UsageParser
    {
        private static readonly DataContractJsonSerializer EventSerializer =
            new DataContractJsonSerializer(typeof(SessionLogEntry));

        private readonly CodexQuotaResponseParser _quotaParser =
            new CodexQuotaResponseParser();

        public QuotaSummary Parse(CodexDataLocation location)
        {
            if (location == null
                || !location.Found
                || location.Files.Count == 0)
            {
                return null;
            }

            LatestRateLimitEvent latest = null;
            foreach (string filePath in location.Files)
            {
                if (!string.Equals(
                    Path.GetExtension(filePath),
                    ".jsonl",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                latest = FindLatestInFile(filePath, latest);
            }

            return latest == null ? null : CreateSummary(latest);
        }

        private LatestRateLimitEvent FindLatestInFile(
            string filePath,
            LatestRateLimitEvent currentLatest)
        {
            try
            {
                DateTimeOffset fallbackTime = new DateTimeOffset(
                    File.GetLastWriteTimeUtc(filePath));
                foreach (string line in File.ReadLines(filePath))
                {
                    currentLatest = ConsiderLine(
                        line,
                        fallbackTime,
                        currentLatest);
                }
            }
            catch (IOException)
            {
                // A live session log can be temporarily unavailable; skip it.
            }
            catch (UnauthorizedAccessException)
            {
                // Session logs are read-only input. An inaccessible file is skipped.
            }

            return currentLatest;
        }

        private static LatestRateLimitEvent ConsiderLine(
            string line,
            DateTimeOffset fallbackTime,
            LatestRateLimitEvent currentLatest)
        {
            if (string.IsNullOrWhiteSpace(line)
                || line.IndexOf(
                    "\"type\":\"event_msg\"",
                    StringComparison.Ordinal) < 0
                || line.IndexOf(
                    "\"type\":\"token_count\"",
                    StringComparison.Ordinal) < 0
                || line.IndexOf(
                    "\"rate_limits\":",
                    StringComparison.Ordinal) < 0)
            {
                return currentLatest;
            }

            SessionLogEntry entry = TryDeserialize(line);
            if (entry == null
                || !string.Equals(
                    entry.Type,
                    "event_msg",
                    StringComparison.OrdinalIgnoreCase)
                || entry.Payload == null
                || !string.Equals(
                    entry.Payload.Type,
                    "token_count",
                    StringComparison.OrdinalIgnoreCase)
                || entry.Payload.RateLimits == null)
            {
                return currentLatest;
            }

            DateTimeOffset timestamp;
            if (!DateTimeOffset.TryParse(
                entry.Timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp))
            {
                timestamp = fallbackTime;
            }

            var candidate = new LatestRateLimitEvent(
                timestamp,
                entry.Payload.RateLimits);
            return currentLatest == null
                   || candidate.Timestamp >= currentLatest.Timestamp
                ? candidate
                : currentLatest;
        }

        private QuotaSummary CreateSummary(LatestRateLimitEvent latest)
        {
            QuotaSummary summary = _quotaParser.ToQuotaSummary(
                new CodexQuotaResponse
                {
                    RateLimits = latest.RateLimits
                });
            if (summary == null)
            {
                return null;
            }

            summary.Account = new CodexAccountInfo
            {
                Type = "local-log",
                PlanType = latest.RateLimits.PlanType
            };
            summary.LastSyncTime = latest.Timestamp == DateTimeOffset.MinValue
                ? DateTime.Now
                : latest.Timestamp.LocalDateTime;
            summary.Status = "日志数据已加载";
            return summary;
        }

        private static SessionLogEntry TryDeserialize(string line)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                using (var stream = new MemoryStream(bytes, false))
                {
                    return EventSerializer.ReadObject(stream) as SessionLogEntry;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [DataContract]
        private sealed class SessionLogEntry
        {
            [DataMember(Name = "timestamp", EmitDefaultValue = false)]
            public string Timestamp { get; set; }

            [DataMember(Name = "type", EmitDefaultValue = false)]
            public string Type { get; set; }

            [DataMember(Name = "payload", EmitDefaultValue = false)]
            public TokenCountPayload Payload { get; set; }
        }

        [DataContract]
        private sealed class TokenCountPayload
        {
            [DataMember(Name = "type", EmitDefaultValue = false)]
            public string Type { get; set; }

            [DataMember(Name = "rate_limits", EmitDefaultValue = false)]
            public CodexRateLimitSnapshot RateLimits { get; set; }
        }

        private sealed class LatestRateLimitEvent
        {
            public LatestRateLimitEvent(
                DateTimeOffset timestamp,
                CodexRateLimitSnapshot rateLimits)
            {
                Timestamp = timestamp;
                RateLimits = rateLimits;
            }

            public DateTimeOffset Timestamp { get; private set; }

            public CodexRateLimitSnapshot RateLimits { get; private set; }
        }
    }
}
