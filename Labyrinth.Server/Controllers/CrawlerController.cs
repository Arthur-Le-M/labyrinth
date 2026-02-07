namespace Labyrinth.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using ApiTypes;
using DTOs;
using Labyrinth.Server.Services;

/// <summary>
/// Controller for managing crawler CRUD operations and movement.
/// Implements the official Labyrinth API specification.
/// </summary>
[ApiController]
[Route("crawlers")]
public class CrawlerController : ControllerBase
{
    private readonly ICrawlerService _crawlerService;
    private readonly IMovementService _movementService;
    private const int MaxCrawlersPerAppKey = 3;

    /// <summary>
    /// Initializes a new instance of the CrawlerController.
    /// </summary>
    public CrawlerController(ICrawlerService crawlerService, IMovementService movementService)
    {
        _crawlerService = crawlerService;
        _movementService = movementService;
    }

    /// <summary>
    /// Gets all crawlers for a specific application.
    /// </summary>
    /// <param name="appKey">The application key (query parameter).</param>
    /// <returns>An array of crawlers belonging to the application.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Crawler[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<Crawler[]> GetCrawlers([FromQuery] string? appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var crawlers = _crawlerService.GetCrawlersByAppKey(appKey);
        return Ok(crawlers.ToArray());
    }

    /// <summary>
    /// Creates a new labyrinth crawler instance (maximum 3 per appKey).
    /// </summary>
    /// <param name="appKey">The unique application key used to authorize the request (query parameter).</param>
    /// <param name="settings">Optional settings to configure the crawler and labyrinth.</param>
    /// <returns>The created crawler.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<Crawler> CreateCrawler(
        [FromQuery] string? appKey,
        [FromBody] Settings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Check crawler limit
        var currentCount = _crawlerService.GetCrawlerCountByAppKey(appKey);
        if (currentCount >= MaxCrawlersPerAppKey)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = $"This app key reached its {MaxCrawlersPerAppKey} instances of simultaneous crawlers",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var crawler = new Crawler
        {
            Id = Guid.NewGuid(),
            X = 1,
            Y = 1,
            Dir = Direction.North,
            Walking = false,
            FacingTile = TileType.Room,
            Bag = Array.Empty<InventoryItem>(),
            Items = Array.Empty<InventoryItem>()
        };

        // Update FacingTile based on actual labyrinth
        crawler.FacingTile = _movementService.GetFacingTile(crawler);

        _crawlerService.AddCrawler(crawler, appKey);

        return CreatedAtAction(nameof(GetCrawler), new { id = crawler.Id }, crawler);
    }

    /// <summary>
    /// Gets a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="appKey">The application key (query parameter).</param>
    /// <returns>The crawler if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Crawler> GetCrawler(Guid id, [FromQuery] string? appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var crawler = _crawlerService.GetCrawler(id);
        
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

        return Ok(crawler);
    }

    /// <summary>
    /// Updates the direction and/or walking state of the crawler.
    /// This is the ONLY way to move the crawler.
    /// Set walking to true to attempt to pass the facing tile.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="appKey">The application key (query parameter).</param>
    /// <param name="request">The update request with direction and/or walking state.</param>
    /// <returns>The updated crawler, or 409 if movement is blocked.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(Crawler), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<Crawler> UpdateCrawler(
        Guid id, 
        [FromQuery] string? appKey,
        [FromBody] CrawlerUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var crawler = _crawlerService.GetCrawler(id);
        
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

        // Apply direction change first if provided
        if (request.Direction.HasValue)
        {
            crawler = new Crawler
            {
                Id = crawler.Id,
                X = crawler.X,
                Y = crawler.Y,
                Dir = request.Direction.Value,
                Walking = crawler.Walking,
                FacingTile = crawler.FacingTile,
                Bag = crawler.Bag,
                Items = crawler.Items
            };
            
            // Update FacingTile based on new direction
            crawler.FacingTile = _movementService.GetFacingTile(crawler);
            _crawlerService.UpdateCrawler(crawler);
        }

        // Handle walking (movement attempt)
        if (request.Walking == true)
        {
            var moveResult = _movementService.TryMove(id);
            
            if (!moveResult.Success)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = moveResult.ErrorMessage ?? "Cannot traverse the facing tile",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return Ok(moveResult.UpdatedCrawler);
        }

        // If walking is false or not provided, just return the crawler
        crawler = _crawlerService.GetCrawler(id)!;
        return Ok(crawler);
    }

    /// <summary>
    /// Deletes a crawler.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <param name="appKey">The application key (query parameter).</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteCrawler(Guid id, [FromQuery] string? appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "A valid app key is required",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var crawler = _crawlerService.GetCrawler(id);
        
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

        _crawlerService.DeleteCrawler(id);
        return NoContent();
    }
}

/// <summary>
/// DTO for updating crawler direction and walking state.
/// Only direction and walking can be modified via PATCH.
/// </summary>
public class CrawlerUpdateRequest
{
    /// <summary>
    /// The new direction for the crawler (optional).
    /// </summary>
    public Direction? Direction { get; set; }

    /// <summary>
    /// Set to true to attempt to walk in the facing direction.
    /// Returns 409 Conflict if the tile is not traversable.
    /// </summary>
    public bool? Walking { get; set; }
}
