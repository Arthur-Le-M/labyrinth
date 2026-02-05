using Labyrinth.Tiles;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Labyrinth;

/// <summary>
/// SharedMap (KnowledgeBase) for concurrent exploration knowledge sharing.
/// Thread-safe storage of discovered tiles accessible by multiple explorers.
/// Supports serialization for export and debugging.
/// </summary>
public class SharedMap
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
    /// </summary>
    /// <param name="position">Position tuple (x, y)</param>
    /// <param name="tile">The tile to store</param>
    public void SetTile((int x, int y) position, Tile tile)
    {
        if (tile == null)
            throw new ArgumentNullException(nameof(tile));

        _tiles.AddOrUpdate(position, tile, (_, __) => tile);

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
        var data = new SharedMapData
        {
            Tiles = _tiles
                .Select(kvp => new TileEntry
                {
                    X = kvp.Key.Item1,
                    Y = kvp.Key.Item2,
                    TileType = kvp.Value.GetType().Name,
                    TileJson = SerializeTile(kvp.Value)
                })
                .ToList()
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserialize a map from JSON.
    /// </summary>
    public static SharedMap FromJson(string json)
    {
        var data = JsonSerializer.Deserialize<SharedMapData>(json)
            ?? throw new InvalidOperationException("Invalid JSON format");

        var map = new SharedMap();
        foreach (var entry in data.Tiles)
        {
            var tile = DeserializeTile(entry.TileType, entry.TileJson);
            map.SetTile((entry.X, entry.Y), tile);
        }

        return map;
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

    private static string SerializeTile(Tile tile)
    {
        return tile.GetType().Name; // Simplified; can be extended for more detail
    }

    private static Tile DeserializeTile(string tileType, string tileJson)
    {
        return tileType switch
        {
            nameof(Room) => new Room(),
            nameof(Wall) => Wall.Singleton,
            nameof(Door) => new Door(),
            nameof(Outside) => Outside.Singleton,
            nameof(Unknown) => new Unknown(),
            _ => throw new InvalidOperationException($"Unknown tile type: {tileType}")
        };
    }

    // Internal DTO for serialization
    private class SharedMapData
    {
        public List<TileEntry> Tiles { get; set; } = new();
    }

    private class TileEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string TileType { get; set; } = string.Empty;
        public string TileJson { get; set; } = string.Empty;
    }
}
