namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// Service implementation for handling crawler movement simulation.
/// Handles collision detection, door validation, and position updates.
/// </summary>
public class MovementService : IMovementService
{
    private readonly ICrawlerService _crawlerService;
    private readonly ILabyrinthService _labyrinthService;
    
    // Default labyrinth ID - in a real implementation this would be per-crawler
    private const string DefaultLabyrinthId = "9x7";

    public MovementService(ICrawlerService crawlerService, ILabyrinthService labyrinthService)
    {
        _crawlerService = crawlerService;
        _labyrinthService = labyrinthService;
    }

    /// <inheritdoc />
    public MovementResult TryMove(Guid crawlerId)
    {
        var crawler = _crawlerService.GetCrawler(crawlerId);
        if (crawler == null)
        {
            return new MovementResult(false, null, "Crawler not found");
        }

        var facingTile = GetFacingTile(crawler);
        
        if (!IsTraversable(facingTile, crawler.Bag))
        {
            return new MovementResult(false, crawler, 
                facingTile == TileType.Door 
                    ? "Door is locked and crawler doesn't have the key" 
                    : "Tile is not traversable");
        }

        // Calculate new position based on direction
        var (newX, newY) = GetNextPosition(crawler.X, crawler.Y, crawler.Dir);

        // Create updated crawler with new position
        var updatedCrawler = new Crawler
        {
            Id = crawler.Id,
            X = newX,
            Y = newY,
            Dir = crawler.Dir,
            Walking = false, // Walking is reset after move
            FacingTile = GetTileAtPosition(newX, newY, crawler.Dir),
            Bag = crawler.Bag,
            Items = GetItemsAtPosition(newX, newY)
        };

        _crawlerService.UpdateCrawler(updatedCrawler);

        return new MovementResult(true, updatedCrawler);
    }

    /// <inheritdoc />
    public TileType GetTileAt(int x, int y)
    {
        var tile = _labyrinthService.GetTile(DefaultLabyrinthId, x, y);
        return tile?.Type ?? TileType.Outside;
    }

    /// <inheritdoc />
    public TileType GetFacingTile(Crawler crawler)
    {
        var (facingX, facingY) = GetNextPosition(crawler.X, crawler.Y, crawler.Dir);
        return GetTileAt(facingX, facingY);
    }

    /// <inheritdoc />
    public bool IsTraversable(TileType tileType, InventoryItem[]? crawlerBag)
    {
        return tileType switch
        {
            TileType.Room => true,
            TileType.Door => HasKey(crawlerBag),
            TileType.Wall => false,
            TileType.Outside => false,
            _ => false
        };
    }

    /// <summary>
    /// Gets the next position based on current position and direction.
    /// </summary>
    private static (int X, int Y) GetNextPosition(int x, int y, Direction direction)
    {
        return direction switch
        {
            Direction.North => (x, y - 1),
            Direction.South => (x, y + 1),
            Direction.East => (x + 1, y),
            Direction.West => (x - 1, y),
            _ => (x, y)
        };
    }

    /// <summary>
    /// Gets the tile type at a position in the direction the crawler would be facing.
    /// </summary>
    private TileType GetTileAtPosition(int x, int y, Direction direction)
    {
        var (facingX, facingY) = GetNextPosition(x, y, direction);
        return GetTileAt(facingX, facingY);
    }

    /// <summary>
    /// Gets items at a specific position.
    /// </summary>
    private InventoryItem[] GetItemsAtPosition(int x, int y)
    {
        // In a full implementation, this would look up items from the labyrinth state
        // For now, return empty array
        return Array.Empty<InventoryItem>();
    }

    /// <summary>
    /// Checks if the crawler has a key in their bag.
    /// </summary>
    private static bool HasKey(InventoryItem[]? bag)
    {
        if (bag == null || bag.Length == 0)
            return false;

        return bag.Any(item => item.Type == ItemType.Key);
    }
}
