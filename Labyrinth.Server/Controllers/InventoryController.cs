namespace Labyrinth.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using ApiTypes;
using Labyrinth.Server.Services;

/// <summary>
/// Controller for managing crawler inventory operations.
/// Implements the official Labyrinth API specification for /crawlers/{id}/bag and /crawlers/{id}/items endpoints.
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
    /// Validates appKey and crawler access.
    /// </summary>
    private ActionResult? ValidateAccess(Guid id, string? appKey, out Crawler? crawler)
    {
        crawler = null;
        
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        crawler = _crawlerService.GetCrawler(id);
        
        if (crawler == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = "Unknown crawler",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!_crawlerService.IsOwner(id, appKey))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "This app key cannot access this crawler",
                Status = StatusCodes.Status403Forbidden
            });
        }

        return null;
    }
    
    /// <summary>
    /// Gets the list of items currently held in the specified crawler's inventory (bag).
    /// </summary>
    /// <param name="id">The unique identifier of the crawler whose inventory bag is to be retrieved.</param>
    /// <param name="appKey">The application key used to authorize the request.</param>
    /// <returns>The items in the crawler's bag.</returns>
    [HttpGet("bag")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<InventoryItem[]> GetBag(Guid id, [FromQuery] string? appKey)
    {
        var validationResult = ValidateAccess(id, appKey, out _);
        if (validationResult != null)
            return validationResult;
        
        var bag = _inventoryService.GetBag(id);
        return Ok(bag ?? Array.Empty<InventoryItem>());
    }
    
    /// <summary>
    /// Updates the inventory bag for the specified crawler.
    /// Set move-required to true to move an item from the bag to the room.
    /// </summary>
    /// <param name="id">The unique identifier of the crawler whose inventory bag is to be updated.</param>
    /// <param name="appKey">The application key used to authorize the request.</param>
    /// <param name="moveRequests">Array of InventoryItem objects with move requirements.</param>
    /// <returns>The updated bag contents.</returns>
    [HttpPut("bag")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<InventoryItem[]> UpdateBag(
        Guid id, 
        [FromQuery] string? appKey, 
        [FromBody] InventoryItem[] moveRequests)
    {
        var validationResult = ValidateAccess(id, appKey, out _);
        if (validationResult != null)
            return validationResult;
        
        var result = _inventoryService.MoveItems(id, moveRequests);
        
        // Check for timeout condition (could be implemented in service)
        if (result == null)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new ProblemDetails
            {
                Title = "Request Timeout",
                Detail = "Inventory access timed out",
                Status = StatusCodes.Status408RequestTimeout
            });
        }
        
        return Ok(result);
    }
    
    /// <summary>
    /// Retrieves the list of inventory items currently associated with the current crawler location (tile).
    /// </summary>
    /// <param name="id">The unique identifier of the crawler whose current tile inventory is to be retrieved.</param>
    /// <param name="appKey">The application key used to authorize the request.</param>
    /// <returns>The items in the room.</returns>
    [HttpGet("items")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<InventoryItem[]> GetRoomItems(Guid id, [FromQuery] string? appKey)
    {
        var validationResult = ValidateAccess(id, appKey, out _);
        if (validationResult != null)
            return validationResult;
        
        var items = _inventoryService.GetRoomItems(id);
        return Ok(items ?? Array.Empty<InventoryItem>());
    }
    
    /// <summary>
    /// Updates the items placed in the tile of the specified crawler.
    /// Set move-required to true to move an item from the room to the crawler's bag.
    /// </summary>
    /// <param name="id">The unique identifier of the crawler whose tile inventory is to be updated.</param>
    /// <param name="appKey">The application key used to authorize the request.</param>
    /// <param name="moveRequests">Array of InventoryItem objects with move requirements.</param>
    /// <returns>The updated tile inventory contents.</returns>
    [HttpPut("items")]
    [ProducesResponseType(typeof(InventoryItem[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<InventoryItem[]> UpdateTileItems(
        Guid id, 
        [FromQuery] string? appKey, 
        [FromBody] InventoryItem[] moveRequests)
    {
        var validationResult = ValidateAccess(id, appKey, out _);
        if (validationResult != null)
            return validationResult;
        
        var result = _inventoryService.MoveRoomItemsToBag(id, moveRequests);
        
        // Result null indicates conflict (inventory changed since last consultation)
        if (result == null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = "Failed to complete item transfer, tile inventory changed since the last consultation",
                Status = StatusCodes.Status409Conflict
            });
        }
        
        return Ok(result);
    }
}
