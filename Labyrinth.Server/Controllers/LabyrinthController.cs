using Microsoft.AspNetCore.Mvc;
using ApiTypes;
using Labyrinth.Server.Services;

namespace Labyrinth.Server.Controllers;

/// <summary>
/// Controller for managing labyrinth and tile operations.
/// Follows Single Responsibility Principle - only handles HTTP requests for labyrinths.
/// </summary>
[ApiController]
[Route("labyrinths/{labyId}")]
public class LabyrinthController : ControllerBase
{
    private readonly ILabyrinthService _labyrinthService;

    /// <summary>
    /// Initializes a new instance of the LabyrinthController.
    /// </summary>
    /// <param name="labyrinthService">The labyrinth service.</param>
    public LabyrinthController(ILabyrinthService labyrinthService)
    {
        _labyrinthService = labyrinthService;
    }

    /// <summary>
    /// Gets all tiles of a labyrinth.
    /// </summary>
    /// <param name="labyId">The labyrinth identifier.</param>
    /// <returns>Array of tiles in the labyrinth.</returns>
    [HttpGet("tiles")]
    [ProducesResponseType(typeof(TileDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TileDto[]> GetTiles(string labyId)
    {
        if (!_labyrinthService.LabyrinthExists(labyId))
        {
            return NotFound();
        }

        var tiles = _labyrinthService.GetAllTiles(labyId);
        return Ok(tiles?.ToArray() ?? Array.Empty<TileDto>());
    }

    /// <summary>
    /// Gets a specific tile at the given coordinates.
    /// </summary>
    /// <param name="labyId">The labyrinth identifier.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The tile at the specified coordinates.</returns>
    [HttpGet("tiles/{x:int}/{y:int}")]
    [ProducesResponseType(typeof(TileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TileDto> GetTile(string labyId, int x, int y)
    {
        if (!_labyrinthService.LabyrinthExists(labyId))
        {
            return NotFound();
        }

        var tile = _labyrinthService.GetTile(labyId, x, y);
        if (tile == null)
        {
            return NotFound();
        }

        return Ok(tile);
    }
}

