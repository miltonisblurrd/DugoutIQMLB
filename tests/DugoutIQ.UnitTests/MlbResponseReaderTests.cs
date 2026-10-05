using DugoutIQ.Domain.Enums;
using DugoutIQ.Infrastructure.BaseballData;

namespace DugoutIQ.UnitTests;

public class MlbResponseReaderTests
{
    [Fact]
    public void ReadSearch_MapsPlayerFields_AndSkipsNonPlayers()
    {
        var json = ReadFixture("mlb-people-search.json");

        var players = MlbResponseReader.ReadSearch(json);

        var player = Assert.Single(players);
        Assert.Equal(592450, player.ExternalId);
        Assert.Equal("Aaron", player.FirstName);
        Assert.Equal("Judge", player.LastName);
        Assert.Equal("RF", player.PositionAbbreviation);
        Assert.Equal(BattingSide.Right, player.Bats);
        Assert.Equal(ThrowingArm.Right, player.Throws);
        Assert.Equal(new DateOnly(1992, 4, 26), player.BirthDate);
    }

    [Fact]
    public void ReadPerson_ExposesCurrentTeamIdWithoutTreatingItAsAFullTeam()
    {
        var json = ReadFixture("mlb-person-with-team.json");

        var player = MlbResponseReader.ReadPlayer(json);
        var teamId = MlbResponseReader.ReadCurrentTeamId(json);

        Assert.NotNull(player);
        Assert.Equal(592450, player.ExternalId);
        Assert.Equal(147, teamId);
    }

    [Fact]
    public void ReadTeam_MapsLeagueDivisionAndAbbreviation()
    {
        var json = ReadFixture("mlb-team.json");

        var team = MlbResponseReader.ReadTeam(json);

        Assert.NotNull(team);
        Assert.Equal(147, team.ExternalId);
        Assert.Equal("New York Yankees", team.Name);
        Assert.Equal("NYY", team.Abbreviation);
        Assert.Equal(League.American, team.League);
        Assert.Equal("East", team.Division);
    }

    [Fact]
    public void ReadSeasonHitting_SumsRegularSeasonSplits_AndIgnoresSpringTraining()
    {
        var json = ReadFixture("mlb-season-hitting.json");

        var hitting = MlbResponseReader.ReadSeasonHitting(json, 2026);

        Assert.NotNull(hitting);
        Assert.Equal(2026, hitting.Season);
        Assert.Equal(66, hitting.Games);
        Assert.Equal(285, hitting.PlateAppearances);
        Assert.Equal(237, hitting.AtBats);
        Assert.Equal(57, hitting.Hits);
        Assert.Equal(10, hitting.Doubles);
        Assert.Equal(1, hitting.Triples);
        Assert.Equal(18, hitting.HomeRuns);
        Assert.Equal(43, hitting.Walks);
        Assert.Equal(85, hitting.Strikeouts);
        Assert.Equal(2, hitting.HitByPitch);
        Assert.Equal(1, hitting.SacrificeFlies);
    }

    private static string ReadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}
