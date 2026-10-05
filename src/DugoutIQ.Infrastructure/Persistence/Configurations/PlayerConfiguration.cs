using DugoutIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DugoutIQ.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players", table =>
            table.HasCheckConstraint("CK_Player_ExternalId", "[ExternalId] > 0"));

        builder.HasKey(player => player.Id);
        builder.Property(player => player.Id).ValueGeneratedNever();

        builder.Property(player => player.FirstName).HasMaxLength(64).IsRequired();
        builder.Property(player => player.LastName).HasMaxLength(64).IsRequired();
        builder.Property(player => player.PositionAbbreviation).HasMaxLength(8).IsRequired();
        builder.Property(player => player.BirthDate).HasColumnType("date");
        builder.Property(player => player.Bats).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(player => player.Throws).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(player => player.ExternalId)
            .IsUnique()
            .HasDatabaseName("IX_Player_ExternalId");

        // Supports ordering and prefix lookups by last name. A contains-search
        // is still a scan; this index is for the sort and for future prefix search.
        builder.HasIndex(player => player.LastName)
            .HasDatabaseName("IX_Player_LastName");

        builder.Navigation(player => player.Seasons)
            .HasField("_seasons")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(player => player.Seasons)
            .WithOne(season => season.Player)
            .HasForeignKey(season => season.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
