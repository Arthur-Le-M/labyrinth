namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// Service interface for managing crawler operations following the Interface Segregation Principle.
/// </summary>
public interface ICrawlerService
{
    /// <summary>
    /// Adds a new crawler to the service.
    /// </summary>
    /// <param name="crawler">The crawler to add.</param>
    void AddCrawler(Crawler crawler);
    
    /// <summary>
    /// Adds a new crawler to the service with an associated application key.
    /// </summary>
    /// <param name="crawler">The crawler to add.</param>
    /// <param name="appKey">The application key associated with the crawler.</param>
    void AddCrawler(Crawler crawler, string appKey);
    
    /// <summary>
    /// Gets a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>The crawler if found, null otherwise.</returns>
    Crawler? GetCrawler(Guid id);
    
    /// <summary>
    /// Gets all crawlers associated with an application key.
    /// </summary>
    /// <param name="appKey">The application key.</param>
    /// <returns>An enumerable of crawlers associated with the application key.</returns>
    IEnumerable<Crawler> GetCrawlersByAppKey(string appKey);
    
    /// <summary>
    /// Checks if a crawler exists.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>True if the crawler exists, false otherwise.</returns>
    bool CrawlerExists(Guid id);
    
    /// <summary>
    /// Updates an existing crawler's state.
    /// </summary>
    /// <param name="crawler">The crawler with updated state.</param>
    void UpdateCrawler(Crawler crawler);
    
    /// <summary>
    /// Deletes a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>True if the crawler was deleted, false if it didn't exist.</returns>
    bool DeleteCrawler(Guid id);
}

