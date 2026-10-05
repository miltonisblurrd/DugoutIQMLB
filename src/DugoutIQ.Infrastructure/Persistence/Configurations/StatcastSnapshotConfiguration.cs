using DugoutIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DugoutIQ.Infrastructure.Persistence.Configurations;

public sealed class StatcastSnapshotConfiguration : IEntityTypeConfiguration<StatcastSnapshot>
{
    public void Configure(EntityTypeBuilder<StatcastSnapshot> builder)
    {
        builder.ToTable("StatcastSnapshots");

        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Id).ValueGeneratedNever();

        // Proportions such as .314, not 31.4. Null means the provider did not send the metric.
        builder.Property(snapshot => snapshot.AverageExitVelocity).HasPrecision(5, 1);
        builder.Property(snapshot => snapshot.MaxExitVelocity).HasPrecision(5, 1);
        builder.Property(snapshot => snapshot.LaunchAngle).HasPrecision(4, 1);
        builder.Property(snapshot => snapshot.BarrelPercentage).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.HardHitPercentage).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.ExpectedBattingAverage).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.ExpectedSlugging).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.ExpectedWoba).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.WhiffPercentage).HasPrecision(6, 3);
        builder.Property(snapshot => snapshot.ChasePercentage).HasPrecision(6, 3);

        // Profile reads the latest row for a player. The trend procedure filters this same pair.
        builder.HasIndex(snapshot => new { snapshot.PlayerId, snapshot.CapturedAt })
            .HasDatabaseName("IX_StatcastSnapshot_PlayerId_CapturedAt");

        // Snapshots are historical observations. Deleting a player must not silently erase them.
        builder.HasOne(snapshot => snapshot.Player)
            .WithMany()
            .HasForeignKey(snapshot => snapshot.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
