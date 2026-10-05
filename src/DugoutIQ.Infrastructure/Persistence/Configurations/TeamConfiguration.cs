using DugoutIQ.Domain.Entities;
using DugoutIQ.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DugoutIQ.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams", table =>
            table.HasCheckConstraint("CK_Team_ExternalId", "[ExternalId] > 0"));

        builder.HasKey(team => team.Id);
        builder.Property(team => team.Id).ValueGeneratedNever();

        builder.Property(team => team.Name).HasMaxLength(100).IsRequired();
        builder.Property(team => team.Abbreviation).HasMaxLength(8).IsRequired();
        builder.Property(team => team.Division).HasMaxLength(32).IsRequired();
        builder.Property(team => team.League).HasConversion<string>().HasMaxLength(16).IsRequired();

        // External id is the provider's team id. It is unique so upserts have a stable key
        // that is not our internal Guid.
        builder.HasIndex(team => team.ExternalId)
            .IsUnique()
            .HasDatabaseName("IX_Team_ExternalId");

        builder.Navigation(team => team.Players)
            .HasField("_players")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(team => team.Players)
            .WithOne(player => player.Team)
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
