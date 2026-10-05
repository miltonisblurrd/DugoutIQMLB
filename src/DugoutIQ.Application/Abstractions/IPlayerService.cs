using DugoutIQ.Application.Players;

namespace DugoutIQ.Application.Abstractions;

public interface IPlayerService
{
    Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(
        string? query,
        CancellationToken cancellationToken);

    Task<PlayerProfileDto> GetProfileAsync(Guid playerId, CancellationToken cancellationToken);
}
