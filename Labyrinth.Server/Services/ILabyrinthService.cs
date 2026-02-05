using ApiTypes;

namespace Labyrinth.Server.Services;

/// <summary>
/// Service interface for managing labyrinth operations following Interface Segregation Principle.
/// </summary>
public interface ILabyrinthService
{
    /// <summary>
    /// Checks if a labyrinth exists.
    /// </summary>
    /// <param name="labyId">The labyrinth identifier.</param>
    /// <returns>True if the labyrinth exists, false otherwise.</returns>
    bool LabyrinthExists(string labyId);

    /// <summary>
    /// Gets all tiles of a labyrinth.
    /// </summary>
    /// <param name="labyId">The labyrinth identifier.</param>
    /// <returns>List of tiles or null if labyrinth doesn't exist.</returns>
    IReadOnlyList<TileDto>? GetAllTiles(string labyId);

    /// <summary>
    /// Gets a specific tile at the given coordinates.
    /// </summary>
    /// <param name="labyId">The labyrinth identifier.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The tile at the given coordinates or null if not found.</returns>
    TileDto? GetTile(string labyId, int x, int y);
}

