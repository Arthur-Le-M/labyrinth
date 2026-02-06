using Labyrinth.Crawl;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Items;
using Labyrinth.Map;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration;

/// <summary>
/// Generic explorer that uses an IExplorationStrategy for decision making.
/// Implements the IExplorer interface for use with MultiExplorer.
/// </summary>
public class StrategyExplorer : IExplorer
{
    private readonly ICrawler _crawler;
    private readonly IExplorationStrategy _strategy;
    private readonly ISharedMap _map;
    private readonly Inventory _inventory;
    private readonly int _maxMoves;
    private bool? _lastMoveSucceeded;
    private (int x, int y)? _lastMoveTarget;
    private int _lastInventoryCount;

    /// <inheritdoc />
    public string Name => $"Explorer-{_strategy.Name}";

    /// <summary>
    /// Gets the crawler being controlled.
    /// </summary>
    public ICrawler Crawler => _crawler;

    /// <summary>
    /// Creates a new strategy-based explorer.
    /// </summary>
    /// <param name="crawler">The crawler to control.</param>
    /// <param name="strategy">The exploration strategy to use.</param>
    /// <param name="map">The shared map for storing discoveries.</param>
    /// <param name="maxMoves">Maximum number of moves before stopping (default: 10000).</param>
    public StrategyExplorer(
        ICrawler crawler,
        IExplorationStrategy strategy,
        ISharedMap map,
        int maxMoves = 10000)
    {
        _crawler = crawler;
        _strategy = strategy;
        _map = map;
        _inventory = new MyInventory();
        _maxMoves = maxMoves;
    }

    /// <summary>
    /// Event raised when the crawler's position changes.
    /// </summary>
    public event EventHandler<CrawlingEventArgs>? PositionChanged;

    /// <summary>
    /// Event raised when the crawler's direction changes.
    /// </summary>
    public event EventHandler<CrawlingEventArgs>? DirectionChanged;

    /// <inheritdoc />
    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < _maxMoves; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var facingType = await _crawler.GetFacingTileTypeAsync(cancellationToken);

            // Update the map with current position (we know it's traversable since we're on it)
            var currentPos = (_crawler.X, _crawler.Y);
            if (!_map.IsKnown(currentPos))
            {
                _map.SetTile(currentPos, new Room());
            }

            // Update the map with the facing tile
            var facingPos = (_crawler.X + _crawler.Direction.DeltaX, _crawler.Y + _crawler.Direction.DeltaY);
            if (!_map.IsKnown(facingPos))
            {
                var facingTile = CreateTileFromType(facingType);
                if (facingTile != null)
                {
                    _map.SetTile(facingPos, facingTile);
                }
            }

            var context = new ExplorationContext(
                currentPos,
                _crawler.Direction,
                _map,
                facingType,
                Target: null,
                LastMoveSucceeded: _lastMoveSucceeded,
                LastMoveTarget: _lastMoveTarget,
                InventoryItemCount: _lastInventoryCount
            );

            var action = _strategy.DecideNextAction(context);

            switch (action)
            {
                case ExplorationAction.Walk:
                {
                    _lastMoveTarget = facingPos;
                    var tileInventory = await _crawler.TryWalk(_inventory, cancellationToken);
                    _lastMoveSucceeded = tileInventory != null;
                    if (tileInventory != null)
                    {
                        await _inventory.TryMoveItemsFrom(
                            tileInventory,
                            tileInventory.ItemTypes.Select(_ => true).ToList()
                        );
                    }
                    _lastInventoryCount = _inventory.ItemTypes.Count();
                    PositionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                    break;
                }
                case ExplorationAction.TurnLeft:
                    _crawler.Direction.TurnLeft();
                    DirectionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                    break;
                case ExplorationAction.TurnRight:
                    _crawler.Direction.TurnRight();
                    DirectionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                    break;
                case ExplorationAction.Stop:
                    return;
            }
        }
    }

    /// <summary>
    /// Create a Tile instance from a Type.
    /// </summary>
    private static Tile? CreateTileFromType(Type tileType)
    {
        if (tileType == typeof(Wall))
            return Wall.Singleton;
        if (tileType == typeof(Room))
            return new Room();
        if (tileType == typeof(Door))
            return new Door();
        if (tileType == typeof(Outside))
            return Outside.Singleton;
        return null;
    }
}
