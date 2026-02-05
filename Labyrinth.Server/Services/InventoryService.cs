namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// Implementation of the inventory service for managing crawler inventories.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ICrawlerService _crawlerService;
    
    /// <summary>
    /// Initializes a new instance of the InventoryService.
    /// </summary>
    /// <param name="crawlerService">The crawler service dependency.</param>
    public InventoryService(ICrawlerService crawlerService)
    {
        _crawlerService = crawlerService;
    }
    
    /// <inheritdoc />
    public InventoryItem[]? GetBag(Guid crawlerId)
    {
        var crawler = _crawlerService.GetCrawler(crawlerId);
        return crawler?.Bag;
    }
    
    /// <inheritdoc />
    public InventoryItem[]? GetRoomItems(Guid crawlerId)
    {
        var crawler = _crawlerService.GetCrawler(crawlerId);
        return crawler?.Items;
    }
    
    /// <inheritdoc />
    public InventoryItem[]? MoveItems(Guid crawlerId, InventoryItem[] moveRequests)
    {
        var crawler = _crawlerService.GetCrawler(crawlerId);
        if (crawler == null)
        {
            return null;
        }
        
        var bag = crawler.Bag?.ToList() ?? new List<InventoryItem>();
        var roomItems = crawler.Items?.ToList() ?? new List<InventoryItem>();
        
        // Process each move request
        for (int i = 0; i < moveRequests.Length && i < bag.Count; i++)
        {
            if (moveRequests[i].MoveRequired == true)
            {
                // Move item from bag to room
                var itemToMove = bag[i];
                roomItems.Add(new InventoryItem { Type = itemToMove.Type });
            }
        }
        
        // Remove moved items from bag (in reverse order to maintain indices)
        var indicesToRemove = new List<int>();
        for (int i = 0; i < moveRequests.Length && i < bag.Count; i++)
        {
            if (moveRequests[i].MoveRequired == true)
            {
                indicesToRemove.Add(i);
            }
        }
        
        foreach (var index in indicesToRemove.OrderByDescending(x => x))
        {
            bag.RemoveAt(index);
        }
        
        // Update crawler state
        crawler.Bag = bag.ToArray();
        crawler.Items = roomItems.ToArray();
        
        if (_crawlerService is CrawlerService crawlerServiceImpl)
        {
            crawlerServiceImpl.UpdateCrawler(crawler);
        }
        
        return crawler.Bag;
    }
}
