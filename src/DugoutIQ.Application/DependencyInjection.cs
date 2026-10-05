using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Players;
using Microsoft.Extensions.DependencyInjection;

namespace DugoutIQ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlayerService, PlayerService>();
        return services;
    }
}
