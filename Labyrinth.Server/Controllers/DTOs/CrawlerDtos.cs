namespace Labyrinth.Server.Controllers.DTOs;

using ApiTypes;

/// <summary>
/// DTO for creating a new crawler.
/// </summary>
public class CreateCrawlerRequest
{
    /// <summary>
    /// The application key for authentication.
    /// </summary>
    public string? AppKey { get; set; }
}

/// <summary>
/// DTO for updating a crawler's position and direction.
/// </summary>
public class UpdateCrawlerRequest
{
    /// <summary>
    /// The application key for authentication.
    /// </summary>
    public string? AppKey { get; set; }
    
    /// <summary>
    /// The new X position (optional).
    /// </summary>
    public int? X { get; set; }
    
    /// <summary>
    /// The new Y position (optional).
    /// </summary>
    public int? Y { get; set; }
    
    /// <summary>
    /// The new direction (optional).
    /// </summary>
    public Direction? Direction { get; set; }
}

/// <summary>
/// DTO for deleting a crawler.
/// </summary>
public class DeleteCrawlerRequest
{
    /// <summary>
    /// The application key for authentication.
    /// </summary>
    public string? AppKey { get; set; }
}

