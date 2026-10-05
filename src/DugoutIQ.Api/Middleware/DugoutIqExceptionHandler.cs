using DugoutIQ.Application.Exceptions;
using DugoutIQ.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DugoutIQ.Api.Middleware;

public sealed class DugoutIqExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DugoutIqExceptionHandler> _logger;

    public DugoutIqExceptionHandler(ILogger<DugoutIqExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problem = CreateProblem(httpContext, exception);
        Log(httpContext, exception, problem.Status ?? StatusCodes.Status500InternalServerError);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblem(HttpContext httpContext, Exception exception)
    {
        ProblemDetails problem = exception switch
        {
            RequestValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Type = "https://dugoutiq.dev/errors/validation",
                Title = "The request is invalid.",
                Status = StatusCodes.Status400BadRequest,
                Detail = validation.Message
            },
            NotFoundException notFound => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/not-found",
                Title = $"{notFound.ResourceType} not found",
                Status = StatusCodes.Status404NotFound,
                Detail = notFound.Message
            },
            ExternalProviderException => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/external-provider",
                Title = "Baseball data is temporarily unavailable.",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = exception.Message
            },
            DomainException => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/domain",
                Title = "The operation violates a baseball rule.",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = exception.Message
            },
            _ when TryGetSqlNumber(exception, out var sqlNumber) && sqlNumber is 2601 or 2627 => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/conflict",
                Title = "The request conflicts with existing data.",
                Status = StatusCodes.Status409Conflict,
                Detail = "The database rejected a duplicate value."
            },
            _ when TryGetSqlNumber(exception, out var sqlNumber) && IsConnectivityError(sqlNumber) => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/database",
                Title = "The database is unavailable.",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "SQL Server could not complete the request."
            },
            _ => new ProblemDetails
            {
                Type = "https://dugoutiq.dev/errors/unexpected",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "The request could not be completed."
            }
        };

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problem;
    }

    private void Log(HttpContext httpContext, Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "RequestFailed RequestPath={RequestPath} StatusCode={StatusCode} CorrelationId={CorrelationId}",
                httpContext.Request.Path.Value,
                statusCode,
                httpContext.TraceIdentifier);
            return;
        }

        _logger.LogWarning(
            exception,
            "RequestRejected RequestPath={RequestPath} StatusCode={StatusCode} CorrelationId={CorrelationId}",
            httpContext.Request.Path.Value,
            statusCode,
            httpContext.TraceIdentifier);
    }

    // These are connection and availability failures. A check constraint or a bad
    // query is a 500, not an outage, and the client still does not see the SQL text.
    private static readonly int[] ConnectivityErrors =
    [
        -2,
        53,
        64,
        233,
        10053,
        10054,
        10060,
        4060,
        40197,
        40501,
        40613,
        40615
    ];

    private static bool IsConnectivityError(int sqlNumber) => ConnectivityErrors.Contains(sqlNumber);

    private static bool TryGetSqlNumber(Exception exception, out int number)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().FullName != "Microsoft.Data.SqlClient.SqlException")
            {
                continue;
            }

            var numberProperty = current.GetType().GetProperty("Number");
            if (numberProperty?.GetValue(current) is int sqlNumber)
            {
                number = sqlNumber;
                return true;
            }
        }

        number = 0;
        return false;
    }
}
