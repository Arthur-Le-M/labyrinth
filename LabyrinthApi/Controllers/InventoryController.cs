namespace LabyrinthApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using ApiTypes;
using LabyrinthApi.Services;

/// <summary>
/// Controller for managing crawler inventory operations.
/// </summary>
[ApiController]
[Route("crawlers/{id}")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICrawlerService _crawlerService;
    
    /// <summary>
    /// Initializes a new instance of the InventoryController.
    /// </summary>
    /// <param name="inventoryService">The inventory service.</param>
    /// <param name="crawlerService">The crawler service.</param>
    public InventoryController(IInventoryService inventoryService, ICrawlerService crawlerService)
    {
        _inventoryService = inventoryService;
        _crawlerService = crawlerService;
    }
    
    /// <summary>
    /// Gets the crawler's bag inventory.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>The items in the crawler's bag.</returns>
    [HttpGet("bag")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<InventoryItem[]> GetBag(Guid id)
    {
        if (!_crawlerService.CrawlerExists(id))
        {
            return NotFound();
        }
        
        var bag = _inventoryService.GetBag(id);
        return Ok(bag ?? Array.Empty<InventoryItem>());
    }
    
    /// <summary>
    /// Moves items in the crawler's bag based on move requirements.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="moveRequests">Array of items with their move requirements.</param>
    /// <returns>The updated bag contents.</returns>
    [HttpPut("bag")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<InventoryItem[]> MoveBagItems(Guid id, [FromBody] InventoryItem[] moveRequests)
    {
        if (!_crawlerService.CrawlerExists(id))
        {
            return NotFound();
        }
        
        var result = _inventoryService.MoveItems(id, moveRequests);
        return Ok(result ?? Array.Empty<InventoryItem>());
    }
    
    /// <summary>
    /// Gets the items available in the room where the crawler is located.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>The items in the room.</returns>
    [HttpGet("items")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<InventoryItem[]> GetRoomItems(Guid id)
    {
        if (!_crawlerService.CrawlerExists(id))
        {
            return NotFound();
        }
        
        var items = _inventoryService.GetRoomItems(id);
        return Ok(items ?? Array.Empty<InventoryItem>());
    }
}

