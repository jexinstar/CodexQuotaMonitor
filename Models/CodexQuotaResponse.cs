using System.Runtime.Serialization;

namespace CodexQuotaMonitor.Models
{
    // Internal representation of the rate_limits object recorded in a
    // token_count session-log event. It is not an app-server response model.
    [DataContract]
    public sealed class CodexQuotaResponse
    {
        [DataMember(Name = "rate_limits", EmitDefaultValue = false)]
        public CodexRateLimitSnapshot RateLimits { get; set; }
    }

    [DataContract]
    public sealed class CodexAccountInfo
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "plan_type", EmitDefaultValue = false)]
        public string PlanType { get; set; }

        public bool? RequiresOpenAiAuth { get; set; }
    }

    [DataContract]
    public sealed class CodexRateLimitSnapshot
    {
        [DataMember(Name = "limit_id", EmitDefaultValue = false)]
        public string LimitId { get; set; }

        [DataMember(Name = "primary", EmitDefaultValue = false)]
        public CodexRateLimitWindow Primary { get; set; }

        [DataMember(Name = "secondary", EmitDefaultValue = false)]
        public CodexRateLimitWindow Secondary { get; set; }

        [DataMember(Name = "plan_type", EmitDefaultValue = false)]
        public string PlanType { get; set; }

        [DataMember(Name = "rate_limit_reached_type", EmitDefaultValue = false)]
        public string RateLimitReachedType { get; set; }
    }

    [DataContract]
    public sealed class CodexRateLimitWindow
    {
        [DataMember(Name = "used_percent", EmitDefaultValue = false)]
        public double? UsedPercent { get; set; }

        [DataMember(Name = "window_minutes", EmitDefaultValue = false)]
        public long? WindowMinutes { get; set; }

        [DataMember(Name = "resets_at", EmitDefaultValue = false)]
        public long? ResetsAt { get; set; }
    }
}
