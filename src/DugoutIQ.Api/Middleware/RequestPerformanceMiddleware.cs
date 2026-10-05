using System.Diagnostics;
using DugoutIQ.Api.Options;
using Microsoft.Extensions.Options;

namespace DugoutIQ.Api.Middleware;

public sealed class RequestPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public RequestPerformanceMiddleware(
        RequestDelegate next,
        ILogger<RequestPerformanceMiddleware> logger,
        IOptions<RequestPerformanceOptions> options)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = options.Value.SlowRequestThresholdMs;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
        }
        finally
        {
            var duration = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (duration >= _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "SlowRequest Method={Method} RequestPath={RequestPath} StatusCode={StatusCode} Duration={Duration} CorrelationId={CorrelationId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    duration,
                    context.TraceIdentifier);
            }
        }
    }
}
