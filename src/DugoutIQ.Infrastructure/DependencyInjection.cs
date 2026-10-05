using System.Net.Http.Headers;
using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Options;
using DugoutIQ.Infrastructure.BaseballData;
using DugoutIQ.Infrastructure.Persistence;
using DugoutIQ.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace DugoutIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BaseballDataOptions>()
            .Bind(configuration.GetSection(BaseballDataOptions.SectionName))
            .ValidateDataAnnotations();

        var connectionString = configuration.GetConnectionString("DugoutIQ");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DugoutIQ is required. Set it in configuration or the environment.");
        }

        services.AddDbContext<DugoutIQDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddMemoryCache();

        services.AddHttpClient(MlbBaseballDataProvider.HttpClientName, (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<BaseballDataOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                // The resilience handler owns the per-attempt timeout. HttpClient.Timeout
                // would otherwise cancel the whole call, including retries.
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DugoutIQ/0.1");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddResilienceHandler("mlb-transient", (pipeline, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<BaseballDataOptions>>().Value;

                // Default retry handles timeouts, HttpRequestException, and 5xx.
                // It does not retry ordinary 4xx responses.
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
            });

        services.AddScoped<IBaseballDataProvider, MlbBaseballDataProvider>();
        return services;
    }
}
