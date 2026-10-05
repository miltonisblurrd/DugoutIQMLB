using System.ComponentModel.DataAnnotations;

namespace DugoutIQ.Application.Options;

public sealed class BaseballDataOptions
{
    public const string SectionName = "BaseballData";

    [Required]
    public string BaseUrl { get; set; } = "https://statsapi.mlb.com/";

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Zero derives the season from the date. A positive value forces that season,
    /// which is useful when you want last year during spring training.
    /// </summary>
    [Range(0, 2100)]
    public int CurrentSeason { get; set; }

    [Range(0, 86_400)]
    public int SearchCacheSeconds { get; set; } = 600;

    [Range(1, 168)]
    public int SeasonRefreshHours { get; set; } = 6;

    [Range(0, 3_600)]
    public int ProfileCacheSeconds { get; set; } = 300;

    public int ResolveSeason(DateOnly today)
    {
        if (CurrentSeason > 0)
        {
            return CurrentSeason;
        }

        // March is a practical boundary: January and February still describe
        // the previous championship season for "current" hitting stats.
        return today.Month >= 3 ? today.Year : today.Year - 1;
    }
}
