using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DugoutIQ.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` so migrations can be created without booting the web host.
/// The connection string comes from the environment, never from source.
/// </summary>
public sealed class DugoutIQDbContextFactory : IDesignTimeDbContextFactory<DugoutIQDbContext>
{
    public DugoutIQDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DUGOUTIQ_SQL_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DugoutIQ");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set DUGOUTIQ_SQL_CONNECTION or ConnectionStrings__DugoutIQ before running dotnet ef. " +
                "Copy .env.example to .env and load it. Do not put the password in source.");
        }

        var options = new DbContextOptionsBuilder<DugoutIQDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new DugoutIQDbContext(options);
    }
}
