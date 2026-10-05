using System.Net;
using System.Text.Json;
using DugoutIQ.Api.Middleware;
using DugoutIQ.Application.Exceptions;
using DugoutIQ.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DugoutIQ.IntegrationTests;

public class DugoutIqExceptionHandlerTests
{
    [Fact]
    public async Task ValidationFailure_Returns400WithoutStackTrace()
    {
        var body = await HandleAsync(new RequestValidationException("q", "Enter a player name."));

        Assert.Equal(StatusCodes.Status400BadRequest, body.Status);
        Assert.Equal("The request is invalid.", body.Document.RootElement.GetProperty("title").GetString());
        Assert.Equal("abc123", body.Document.RootElement.GetProperty("traceId").GetString());
        Assert.Contains("Enter a player name.", body.Json, StringComparison.Ordinal);
        AssertNoInternals(body.Json);
    }

    [Fact]
    public async Task MissingPlayer_Returns404()
    {
        var body = await HandleAsync(new NotFoundException("Player", "8f0c2c5e-1b2a-4d3e-9f70-112233445566"));

        Assert.Equal(StatusCodes.Status404NotFound, body.Status);
        Assert.Equal("Player not found", body.Document.RootElement.GetProperty("title").GetString());
        Assert.Contains("8f0c2c5e-1b2a-4d3e-9f70-112233445566", body.Json, StringComparison.Ordinal);
        AssertNoInternals(body.Json);
    }

    [Fact]
    public async Task ProviderFailure_Returns503WithSafeDetail()
    {
        var exception = new ExternalProviderException(
            "MlbStatsApi",
            "SearchPlayers",
            "The baseball data provider could not be reached.",
            new HttpRequestException("statsapi.mlb.com connection reset"));

        var body = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, body.Status);
        Assert.Equal(
            "The baseball data provider could not be reached.",
            body.Document.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("statsapi.mlb.com", body.Json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpRequestException", body.Json, StringComparison.Ordinal);
        AssertNoInternals(body.Json);
    }

    [Fact]
    public async Task DomainRuleFailure_Returns422()
    {
        var body = await HandleAsync(new DomainException("Counting stats cannot be negative."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, body.Status);
        Assert.Equal("Counting stats cannot be negative.", body.Document.RootElement.GetProperty("detail").GetString());
        AssertNoInternals(body.Json);
    }

    [Fact]
    public async Task UnexpectedFailure_Returns500WithoutExceptionText()
    {
        var body = await HandleAsync(new InvalidOperationException("Password=SuperSecret; Server=sql.internal"));

        Assert.Equal(StatusCodes.Status500InternalServerError, body.Status);
        Assert.Equal("The request could not be completed.", body.Document.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("SuperSecret", body.Json, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body.Json, StringComparison.Ordinal);
        AssertNoInternals(body.Json);
    }

    [Fact]
    public async Task ClientCancellation_IsNotConvertedToAnErrorResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestAborted = new CancellationToken(canceled: true);
        var handler = new DugoutIqExceptionHandler(NullLogger<DugoutIqExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(context.RequestAborted),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static async Task<HandledProblem> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "abc123",
            Response = { Body = new MemoryStream() }
        };
        var handler = new DugoutIqExceptionHandler(NullLogger<DugoutIqExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return new HandledProblem(context.Response.StatusCode, json, JsonDocument.Parse(json));
    }

    private static void AssertNoInternals(string json)
    {
        Assert.DoesNotContain("at DugoutIQ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", json, StringComparison.Ordinal);
    }

    private sealed record HandledProblem(int Status, string Json, JsonDocument Document);
}
