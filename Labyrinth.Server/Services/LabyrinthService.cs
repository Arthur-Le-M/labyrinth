using System.Text.Json;
using ApiTypes;

namespace Labyrinth.Server.Services;

/// <summary>
/// Service implementation for managing labyrinths, loading from JSON files.
/// Follows Single Responsibility Principle - only handles labyrinth data operations.
/// </summary>
public class LabyrinthService : ILabyrinthService
{
    private readonly Dictionary<string, LabyrinthData> _labyrinths = new();

    public LabyrinthService()
    {
        LoadLabyrinths();
    }

    /// <inheritdoc />
    public bool LabyrinthExists(string labyId)
    {
        if (string.IsNullOrEmpty(labyId))
            return false;
        return _labyrinths.ContainsKey(labyId);
    }

    /// <inheritdoc />
    public IReadOnlyList<TileDto>? GetAllTiles(string labyId)
    {
        if (!LabyrinthExists(labyId))
            return null;

        return _labyrinths[labyId].Tiles;
    }

    /// <inheritdoc />
    public TileDto? GetTile(string labyId, int x, int y)
    {
        if (!LabyrinthExists(labyId))
            return null;

        var labyrinth = _labyrinths[labyId];
        
        if (x < 0 || x >= labyrinth.Width || y < 0 || y >= labyrinth.Height)
            return null;

        return labyrinth.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);
    }

    /// <summary>
    /// Loads labyrinths from JSON files.
    /// </summary>
    private void LoadLabyrinths()
    {
        LoadLabyrinthFromJson("9x7", "labyrinth9x7.json");
        LoadLabyrinthFromJson("17x19", "labyrinth17x19.json");
    }

    /// <summary>
    /// Loads a single labyrinth from a JSON file.
    /// </summary>
    private void LoadLabyrinthFromJson(string labyId, string filename)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(basePath, filename),
            Path.Combine(basePath, "..", "..", "..", "..", "Labyrinth", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Labyrinth", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "Labyrinth", filename)
        };

        string? jsonContent = null;
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                jsonContent = File.ReadAllText(fullPath);
                break;
            }
        }

        if (jsonContent == null)
        {
            // Use embedded preview data as fallback
            var data = GetEmbeddedLabyrinthData(labyId);
            if (data != null)
            {
                _labyrinths[labyId] = data;
            }
            return;
        }

        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("preview", out var previewElement))
        {
            var preview = previewElement.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .Where(s => !s.Contains("(") && s.Length > 0) // Filter out legend lines
                .ToList();

            var labyrinthData = ParsePreview(preview);
            _labyrinths[labyId] = labyrinthData;
        }
        else if (root.TryGetProperty("-", out var dashElement))
        {
            var preview = dashElement.EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToList();

            var labyrinthData = ParsePreview(preview);
            _labyrinths[labyId] = labyrinthData;
        }
    }

    /// <summary>
    /// Parses the preview ASCII representation into tile data.
    /// </summary>
    private static LabyrinthData ParsePreview(List<string> preview)
    {
        var tiles = new List<TileDto>();
        var height = preview.Count;
        var width = 0;

        for (var y = 0; y < preview.Count; y++)
        {
            var line = preview[y];
            var x = 0;
            
            foreach (var ch in line)
            {
                var tileType = ch switch
                {
                    '#' => TileType.Wall,
                    ' ' => TileType.Room,
                    'd' or 'D' => TileType.Door,
                    '/' => TileType.Door,
                    'k' or 'K' => TileType.Room, // Key is in a room
                    _ => TileType.Room // Default to room for unknown characters
                };

                tiles.Add(new TileDto(x, y, tileType));
                x++;
            }

            if (x > width)
                width = x;
        }

        return new LabyrinthData(width, height, tiles);
    }

    /// <summary>
    /// Gets embedded labyrinth data as fallback when JSON files are not found.
    /// </summary>
    private static LabyrinthData? GetEmbeddedLabyrinthData(string labyId)
    {
        return labyId switch
        {
            "9x7" => ParsePreview(new List<string>
            {
                " # # # d # # # # #",
                " # k             #",
                " #   # # # # #   #",
                " #   /   #   /   #",
                " #   # # # # #   #",
                " D K     #       #",
                " # # # # # # # # #"
            }),
            "17x19" => ParsePreview(new List<string>
            {
                "#################",
                "#               #",
                "# #####/####### #",
                "# #        k  #k#",
                "# # ###/##### # #",
                "# / #    k  # # #",
                "# # # ##### # # #",
                "# # / / # / # # #",
                "# # # ##### # # #",
                "# # #  k#   # # #",
                "# # ######### # #",
                "# #        k# # #",
                "# ###############",
                "#               /",
                "#################"
            }),
            _ => null
        };
    }

    /// <summary>
    /// Internal data structure for storing labyrinth information.
    /// </summary>
    private record LabyrinthData(int Width, int Height, List<TileDto> Tiles);
}

