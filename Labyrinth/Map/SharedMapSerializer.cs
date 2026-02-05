using Labyrinth.Tiles;
using System.Text.Json;

namespace Labyrinth.Map;

/// <summary>
/// Handles serialization and deserialization of SharedMap.
/// Separates persistence concerns from the core SharedMap logic (SRP).
/// </summary>
public class SharedMapSerializer
{
    /// <summary>
    /// Serialize a SharedMap to JSON format.
    /// </summary>
    public string Serialize(ISharedMap map)
    {
        var data = new SharedMapData
        {
            Tiles = map.ExportAllTiles()
                .Select(item => new TileEntry
                {
                    X = item.position.x,
                    Y = item.position.y,
                    TileType = item.tile.GetType().Name,
                    TileJson = SerializeTile(item.tile)
                })
                .ToList()
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Deserialize a SharedMap from JSON format.
    /// </summary>
    public SharedMap Deserialize(string json)
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

    private static string SerializeTile(Tile tile)
    {
        return tile.GetType().Name;
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
