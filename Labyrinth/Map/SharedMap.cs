using Labyrinth.Tiles;
using System.Collections.Concurrent;

namespace Labyrinth.Map;

/// <summary>
/// SharedMap (KnowledgeBase) for concurrent exploration knowledge sharing.
/// Thread-safe storage of discovered tiles accessible by multiple explorers.
/// Supports serialization for export and debugging.
/// Updates door states when they are opened.
/// </summary>
public class SharedMap : ISharedMap
{
    private readonly ConcurrentDictionary<(int, int), Tile> _tiles = new();
    private readonly ReaderWriterLockSlim _boundsLock = new();
    private int _minX = int.MaxValue;
    private int _maxX = int.MinValue;
    private int _minY = int.MaxValue;
    private int _maxY = int.MinValue;

    /// <summary>
    /// Store a tile at a specific position.
    /// Thread-safe for concurrent writes.
    /// Special handling: if a Room replaces a Door, it means the door was opened.
    /// </summary>
    /// <param name="position">Position tuple (x, y)</param>
    /// <param name="tile">The tile to store</param>
    public void SetTile((int x, int y) position, Tile tile)
    {
        if (tile == null)
            throw new ArgumentNullException(nameof(tile));

        _tiles.AddOrUpdate(
            position,
            tile, // Add new tile if not exists
            (key, existingTile) =>
            {
                // If we're replacing a Door with a Room, it means the door was successfully traversed
                // Keep the Room to indicate it's now passable
                if (existingTile is Door && tile is Room)
                {
                    return tile; // Door is now open/traversable
                }
                
                // If we're trying to add a Door but a Room already exists, keep the Room
                // (the door was already opened by another explorer)
                if (existingTile is Room && tile is Door)
                {
                    return existingTile; // Keep it as traversable
                }

                // If both are doors, keep the one that's opened
                if (existingTile is Door existingDoor && tile is Door newDoor)
                {
                    // Opened door has items in LocalInventory (the key is in the door)
                    // Locked door has empty LocalInventory (key was removed)
                    if (newDoor.IsOpened && !existingDoor.IsOpened)
                    {
                        return newDoor; // Update to opened door
                    }
                    if (existingDoor.IsOpened)
                    {
                        return existingDoor; // Keep opened door
                    }
                    return newDoor; // Otherwise use new info
                }
                
                // For other cases, update with new tile
                return tile;
            });

        // Update bounds
        _boundsLock.EnterWriteLock();
        try
        {
            _minX = Math.Min(_minX, position.x);
            _maxX = Math.Max(_maxX, position.x);
            _minY = Math.Min(_minY, position.y);
            _maxY = Math.Max(_maxY, position.y);
        }
        finally
        {
            _boundsLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Retrieve a tile from a specific position.
    /// Thread-safe for concurrent reads.
    /// </summary>
    /// <param name="position">Position tuple (x, y)</param>
    /// <returns>The tile at the position, or null if unknown</returns>
    public Tile? GetTile((int x, int y) position)
    {
        _tiles.TryGetValue(position, out var tile);
        return tile;
    }

    /// <summary>
    /// Get all discovered tiles.
    /// Returns a snapshot of the current map.
    /// </summary>
    /// <returns>List of (position, tile) tuples</returns>
    public List<((int x, int y) position, Tile tile)> ExportAllTiles()
    {
        return _tiles
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    /// <summary>
    /// Get the bounds of the explored area.
    /// </summary>
    /// <returns>Tuple of (minX, maxX, minY, maxY), or defaults if empty</returns>
    public (int minX, int maxX, int minY, int maxY) GetKnownBounds()
    {
        _boundsLock.EnterReadLock();
        try
        {
            if (_tiles.IsEmpty)
                return (0, 0, 0, 0);
            return (_minX, _maxX, _minY, _maxY);
        }
        finally
        {
            _boundsLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Check if a position has been explored.
    /// </summary>
    /// <param name="position">Position tuple (x, y)</param>
    /// <returns>True if tile is known, false otherwise</returns>
    public bool IsKnown((int x, int y) position)
    {
        return _tiles.ContainsKey(position);
    }

    /// <summary>
    /// Get the count of discovered tiles.
    /// </summary>
    public int TileCount => _tiles.Count;

    /// <summary>
    /// Serialize the map to JSON.
    /// </summary>
    public string ToJson()
    {
        var serializer = new SharedMapSerializer();
        return serializer.Serialize(this);
    }

    /// <summary>
    /// Deserialize a map from JSON.
    /// </summary>
    public static SharedMap FromJson(string json)
    {
        var serializer = new SharedMapSerializer();
        return serializer.Deserialize(json);
    }

    /// <summary>
    /// Clear all explored data.
    /// </summary>
    public void Clear()
    {
        _tiles.Clear();
        _boundsLock.EnterWriteLock();
        try
        {
            _minX = int.MaxValue;
            _maxX = int.MinValue;
            _minY = int.MaxValue;
            _maxY = int.MinValue;
        }
        finally
        {
            _boundsLock.ExitWriteLock();
        }
    }
}