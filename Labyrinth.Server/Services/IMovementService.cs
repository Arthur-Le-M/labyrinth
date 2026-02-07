namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// Service interface for handling crawler movement simulation.
/// Follows Single Responsibility Principle - only handles movement logic.
/// </summary>
public interface IMovementService
{
    /// <summary>
    /// Attempts to move the crawler in its current facing direction.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <returns>A result indicating success/failure and the updated crawler state.</returns>
    MovementResult TryMove(Guid crawlerId);

    /// <summary>
    /// Gets the tile type at the specified position.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The tile type at the position.</returns>
    TileType GetTileAt(int x, int y);

    /// <summary>
    /// Gets the tile type that the crawler is currently facing.
    /// </summary>
    /// <param name="crawler">The crawler.</param>
    /// <returns>The tile type the crawler is facing.</returns>
    TileType GetFacingTile(Crawler crawler);

    /// <summary>
    /// Checks if a tile is traversable (can be walked through).
    /// </summary>
    /// <param name="tileType">The tile type to check.</param>
    /// <param name="crawlerBag">The crawler's inventory (for key checking).</param>
    /// <returns>True if the tile can be traversed.</returns>
    bool IsTraversable(TileType tileType, InventoryItem[]? crawlerBag);
}

/// <summary>
/// Result of a movement attempt.
/// </summary>
public record MovementResult(bool Success, Crawler? UpdatedCrawler, string? ErrorMessage = null);
