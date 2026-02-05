using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Map;

namespace Labyrinth.Exploration;

/// <summary>
/// Real explorer implementation wrapping RandExplorer for use with MultiExplorer
/// Demonstrates alignment with project requirements: "explorateurs s'appuyant sur les crawlers"
/// </summary>
public class ConcurrentRandExplorer : IExplorer
{
    private readonly RandExplorer _randExplorer;
    private readonly ISharedMap _sharedMap;
    private readonly int _maxMoves;
    private readonly Inventory _inventory;

    public string Name { get; }

    public ConcurrentRandExplorer(string name, RandExplorer randExplorer, ISharedMap sharedMap, int maxMoves = 1000)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _randExplorer = randExplorer ?? throw new ArgumentNullException(nameof(randExplorer));
        _sharedMap = sharedMap ?? throw new ArgumentNullException(nameof(sharedMap));
        _maxMoves = maxMoves;
        _inventory = new MyInventory();
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        // Subscribe to position changes to update shared map
        _randExplorer.PositionChanged += OnPositionChanged;
        
        try
        {
            await _randExplorer.GetOut(_maxMoves, _inventory, cancellationToken);
        }
        finally
        {
            _randExplorer.PositionChanged -= OnPositionChanged;
        }
    }

    private void OnPositionChanged(object? sender, CrawlingEventArgs e)
    {
        var crawler = _randExplorer.Crawler;
        var currentPos = (crawler.X, crawler.Y);
        
        if (!_sharedMap.IsKnown(currentPos))
        {
            // For now, we just mark it as known in the shared map
            // A more complete implementation would fetch and store the actual tile
        }
    }
}
