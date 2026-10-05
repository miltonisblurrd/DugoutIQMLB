using System.Text.Json;

namespace DugoutIQ.Infrastructure.BaseballData;

internal static class MlbJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal sealed class MlbPeopleEnvelope
{
    public List<MlbPerson> People { get; set; } = [];
}

internal sealed class MlbPerson
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? BirthDate { get; set; }

    public bool? IsPlayer { get; set; }

    public MlbPosition? PrimaryPosition { get; set; }

    public MlbCode? BatSide { get; set; }

    public MlbCode? PitchHand { get; set; }

    public MlbTeamStub? CurrentTeam { get; set; }
}

internal sealed class MlbPosition
{
    public string? Abbreviation { get; set; }
}

internal sealed class MlbCode
{
    public string? Code { get; set; }
}

internal sealed class MlbTeamStub
{
    public int Id { get; set; }

    public string? Name { get; set; }
}

internal sealed class MlbTeamsEnvelope
{
    public List<MlbTeam> Teams { get; set; } = [];
}

internal sealed class MlbTeam
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Abbreviation { get; set; }

    public MlbNamed? League { get; set; }

    public MlbNamed? Division { get; set; }
}

internal sealed class MlbNamed
{
    public string? Name { get; set; }
}

internal sealed class MlbStatsEnvelope
{
    public List<MlbStatBlock> Stats { get; set; } = [];
}

internal sealed class MlbStatBlock
{
    public List<MlbSplit> Splits { get; set; } = [];
}

internal sealed class MlbSplit
{
    public string? Season { get; set; }

    public string? GameType { get; set; }

    public JsonElement Stat { get; set; }
}
