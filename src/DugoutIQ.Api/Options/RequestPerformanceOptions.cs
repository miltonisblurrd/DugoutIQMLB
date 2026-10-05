using System.ComponentModel.DataAnnotations;

namespace DugoutIQ.Api.Options;

public sealed class RequestPerformanceOptions
{
    public const string SectionName = "RequestPerformance";

    [Range(1, 60_000)]
    public int SlowRequestThresholdMs { get; set; } = 1000;
}
