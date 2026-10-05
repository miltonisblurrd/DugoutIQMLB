using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Players;
using Microsoft.AspNetCore.Mvc;

namespace DugoutIQ.Api.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<PlayerSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<PlayerSearchResultDto>>> Search(
        [FromQuery(Name = "q")] string? q,
        CancellationToken cancellationToken)
    {
        var results = await _playerService.SearchAsync(q, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var profile = await _playerService.GetProfileAsync(id, cancellationToken);
        return Ok(profile);
    }
}
