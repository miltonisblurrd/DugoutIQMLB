using DugoutIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DugoutIQ.Infrastructure.Persistence;

public sealed class DugoutIQDbContext : DbContext
{
    public DugoutIQDbContext(DbContextOptions<DugoutIQDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<PlayerSeason> PlayerSeasons => Set<PlayerSeason>();

    public DbSet<StatcastSnapshot> StatcastSnapshots => Set<StatcastSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DugoutIQDbContext).Assembly);
    }
}
