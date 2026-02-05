namespace ApiTypes;

/// <summary>
/// Data Transfer Object representing a tile in the labyrinth.
/// </summary>
/// <param name="X">The X coordinate of the tile.</param>
/// <param name="Y">The Y coordinate of the tile.</param>
/// <param name="Type">The type of the tile.</param>
public record TileDto(int X, int Y, TileType Type);

