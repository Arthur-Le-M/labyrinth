using Labyrinth.Crawl;
using Labyrinth.Map;

namespace Labyrinth.Exploration.Strategies;

/// <summary>
/// Context passed to strategy for decision making.
/// Contains all the information a strategy needs to decide the next action.
/// </summary>
/// <param name="CurrentPosition">Current position of the crawler (x, y).</param>
/// <param name="CurrentDirection">Current direction the crawler is facing.</param>
/// <param name="KnownMap">The shared map containing discovered tiles.</param>
/// <param name="FacingTileType">The type of the tile in front of the crawler.</param>
/// <param name="Target">Optional target position for pathfinding strategies.</param>
public record ExplorationContext(
    (int x, int y) CurrentPosition,
    Direction CurrentDirection,
    ISharedMap KnownMap,
    Type FacingTileType,
    (int x, int y)? Target = null,
    bool? LastMoveSucceeded = null,
    (int x, int y)? LastMoveTarget = null,
    int InventoryItemCount = 0
);
