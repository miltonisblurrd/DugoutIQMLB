using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DugoutIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    League = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Division = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.CheckConstraint("CK_Team_ExternalId", "[ExternalId] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PositionAbbreviation = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Bats = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Throws = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.CheckConstraint("CK_Player_ExternalId", "[ExternalId] > 0");
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSeasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    PlateAppearances = table.Column<int>(type: "int", nullable: false),
                    AtBats = table.Column<int>(type: "int", nullable: false),
                    Hits = table.Column<int>(type: "int", nullable: false),
                    Doubles = table.Column<int>(type: "int", nullable: false),
                    Triples = table.Column<int>(type: "int", nullable: false),
                    HomeRuns = table.Column<int>(type: "int", nullable: false),
                    Walks = table.Column<int>(type: "int", nullable: false),
                    Strikeouts = table.Column<int>(type: "int", nullable: false),
                    HitByPitch = table.Column<int>(type: "int", nullable: false),
                    SacrificeFlies = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasons", x => x.Id);
                    table.CheckConstraint("CK_PlayerSeason_Season", "[Season] BETWEEN 1876 AND 2100");
                    table.ForeignKey(
                        name: "FK_PlayerSeasons_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatcastSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AverageExitVelocity = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    MaxExitVelocity = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    BarrelPercentage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    HardHitPercentage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    LaunchAngle = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    ExpectedBattingAverage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    ExpectedSlugging = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    ExpectedWoba = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    WhiffPercentage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    ChasePercentage = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatcastSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatcastSnapshots_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Player_ExternalId",
                table: "Players",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_LastName",
                table: "Players",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeason_PlayerId_Season",
                table: "PlayerSeasons",
                columns: new[] { "PlayerId", "Season" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatcastSnapshot_PlayerId_CapturedAt",
                table: "StatcastSnapshots",
                columns: new[] { "PlayerId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Team_ExternalId",
                table: "Teams",
                column: "ExternalId",
                unique: true);

            // Same text as database/procedures/sp_GetPlayerTrendSummary.sql.
            // StatcastSnapshots already exists above. Nothing calls this procedure yet.
            migrationBuilder.Sql(
                """
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetPlayerTrendSummary;");

            migrationBuilder.DropTable(
                name: "PlayerSeasons");

            migrationBuilder.DropTable(
                name: "StatcastSnapshots");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
