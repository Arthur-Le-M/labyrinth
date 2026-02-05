namespace Labyrinth.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using ApiTypes;
using DTOs;
using Labyrinth.Server.Services;

/// <summary>
/// Controller for managing crawler CRUD operations.
/// Follows Single Responsibility Principle - handles only crawler lifecycle operations.
/// </summary>
[ApiController]
[Route("crawlers")]
public class CrawlerController : ControllerBase
{
    private readonly ICrawlerService _crawlerService;

    /// <summary>
    /// Initializes a new instance of the CrawlerController.
    /// </summary>
    /// <param name="crawlerService">The crawler service for managing crawlers.</param>
    public CrawlerController(ICrawlerService crawlerService)
    {
        _crawlerService = crawlerService;
    }

    /// <summary>
    /// Gets all crawlers for a specific application.
    /// </summary>
    /// <param name="appKey">The application key to filter crawlers.</param>
    /// <returns>An array of crawlers belonging to the application.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Crawler[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Crawler[]> GetCrawlers([FromQuery] string? appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return BadRequest("AppKey is required");
        }

        var crawlers = _crawlerService.GetCrawlersByAppKey(appKey);
        return Ok(crawlers.ToArray());
    }

    /// <summary>
    /// Creates a new crawler.
    /// </summary>
    /// <param name="request">The create crawler request containing the app key.</param>
    /// <returns>The created crawler.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Crawler> CreateCrawler([FromBody] CreateCrawlerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppKey))
        {
            return BadRequest("AppKey is required");
        }

        var crawler = new Crawler
        {
            Id = Guid.NewGuid(),
            X = 0,
            Y = 0,
            Dir = Direction.North,
            Walking = false,
            FacingTile = TileType.Room,
            Bag = Array.Empty<InventoryItem>(),
            Items = Array.Empty<InventoryItem>()
        };

        _crawlerService.AddCrawler(crawler, request.AppKey);

        return CreatedAtAction(nameof(GetCrawler), new { id = crawler.Id }, crawler);
    }

    /// <summary>
    /// Gets a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>The crawler if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Crawler> GetCrawler(Guid id)
    {
        var crawler = _crawlerService.GetCrawler(id);
        
        if (crawler == null)
        {
            return NotFound();
        }

        return Ok(crawler);
    }

    /// <summary>
    /// Updates a crawler's position and/or direction.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="request">The update request with optional position and direction changes.</param>
    /// <returns>The updated crawler.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Crawler> UpdateCrawler(Guid id, [FromBody] UpdateCrawlerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppKey))
        {
            return BadRequest("AppKey is required");
        }

        var crawler = _crawlerService.GetCrawler(id);
        
        if (crawler == null)
        {
            return NotFound();
        }

        // Apply partial updates - only update fields that are provided
        var updatedCrawler = new Crawler
        {
            Id = crawler.Id,
            X = request.X ?? crawler.X,
            Y = request.Y ?? crawler.Y,
            Dir = request.Direction ?? crawler.Dir,
            Walking = crawler.Walking,
            FacingTile = crawler.FacingTile,
            Bag = crawler.Bag,
            Items = crawler.Items
        };

        _crawlerService.UpdateCrawler(updatedCrawler);

        return Ok(updatedCrawler);
    }

    /// <summary>
    /// Deletes a crawler.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="request">The delete request containing the app key.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteCrawler(Guid id, [FromBody] DeleteCrawlerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppKey))
        {
            return BadRequest("AppKey is required");
        }

        if (!_crawlerService.DeleteCrawler(id))
        {
            return NotFound();
        }

        return NoContent();
    }
}
