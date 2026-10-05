-- Applied by the InitialCreate EF migration. Keep the two copies identical.
-- Nothing in the application calls this procedure yet.
-- Aggregates stored Statcast observations for one player over a half-open
-- time window [StartDate, EndDate). AVG ignores null metric columns, so a
-- snapshot that only has expected wOBA does not become a zero exit velocity.
-- SnapshotCount still counts that row.
CREATE OR ALTER PROCEDURE dbo.sp_GetPlayerTrendSummary
    @PlayerId uniqueidentifier,
    @StartDate datetimeoffset,
    @EndDate datetimeoffset
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AVG(AverageExitVelocity) AS AverageExitVelocity,
        AVG(BarrelPercentage) AS BarrelPercentage,
        AVG(HardHitPercentage) AS HardHitPercentage,
        AVG(ExpectedWoba) AS AverageXwOBA,
        COUNT(*) AS SnapshotCount
    FROM dbo.StatcastSnapshots
    WHERE PlayerId = @PlayerId
      AND CapturedAt >= @StartDate
      AND CapturedAt < @EndDate;
END
