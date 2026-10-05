using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DugoutIQ.IntegrationTests;

public class PlayerSearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    static PlayerSearchEndpointTests()
    {
        // The host requires a connection string at startup. This value is not a
        // database credential, and this test fails validation before any query.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DugoutIQ",
            "Server=127.0.0.1,1433;Database=DugoutIQ;User Id=sa;Password=integration-test-unused;TrustServerCertificate=True;Encrypt=False");
    }

    public PlayerSearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_WhenQueryIsMissing_ReturnsProblemDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/players/search");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Enter a player name.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.Ordinal);
        Assert.DoesNotContain("at DugoutIQ", body, StringComparison.Ordinal);
    }
}
