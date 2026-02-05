using Labyrinth.Tiles;

namespace Labyrinth.Map;

/// <summary>
/// Interface for shared knowledge base accessible by multiple explorers.
/// Provides thread-safe storage and retrieval of discovered tiles.
/// </summary>
public interface ISharedMap
{
    /// <summary>
    /// Store a tile at a specific position.
    /// Thread-safe for concurrent writes.
    /// </summary>
    void SetTile((int x, int y) position, Tile tile);

    /// <summary>
    /// Retrieve a tile from a specific position.
    /// Thread-safe for concurrent reads.
    /// </summary>
    Tile? GetTile((int x, int y) position);

    /// <summary>
    /// Get all discovered tiles as a snapshot.
    /// </summary>
    List<((int x, int y) position, Tile tile)> ExportAllTiles();

    /// <summary>
    /// Get the bounds of the explored area.
    /// </summary>
    (int minX, int maxX, int minY, int maxY) GetKnownBounds();

    /// <summary>
    /// Check if a position has been explored.
    /// </summary>
    bool IsKnown((int x, int y) position);

    /// <summary>
    /// Get the count of discovered tiles.
    /// </summary>
    int TileCount { get; }

    /// <summary>
    /// Clear all explored data.
    /// </summary>
    void Clear();
}
