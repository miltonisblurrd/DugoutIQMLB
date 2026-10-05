using DugoutIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DugoutIQ.Infrastructure.Persistence.Configurations;

public sealed class PlayerSeasonConfiguration : IEntityTypeConfiguration<PlayerSeason>
{
    public void Configure(EntityTypeBuilder<PlayerSeason> builder)
    {
        builder.ToTable("PlayerSeasons", table =>
            table.HasCheckConstraint("CK_PlayerSeason_Season", "[Season] BETWEEN 1876 AND 2100"));

        builder.HasKey(season => season.Id);
        builder.Property(season => season.Id).ValueGeneratedNever();

        // One counting-stat line per player per year. Mid-season trades are summed
        // into this row rather than stored as separate club splits.
        builder.HasIndex(season => new { season.PlayerId, season.Season })
            .IsUnique()
            .HasDatabaseName("IX_PlayerSeason_PlayerId_Season");
    }
}
