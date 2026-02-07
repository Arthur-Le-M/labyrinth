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

    /// <summary>
    /// Gets the count of crawlers for a specific application key.
    /// </summary>
    /// <param name="appKey">The application key.</param>
    /// <returns>The number of crawlers associated with the app key.</returns>
    int GetCrawlerCountByAppKey(string appKey);

    /// <summary>
    /// Gets the application key associated with a crawler.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <returns>The app key if found, null otherwise.</returns>
    string? GetAppKeyForCrawler(Guid crawlerId);

    /// <summary>
    /// Checks if the app key owns the crawler.
    /// </summary>
    /// <param name="crawlerId">The crawler's unique identifier.</param>
    /// <param name="appKey">The application key to check.</param>
    /// <returns>True if the app key owns the crawler.</returns>
    bool IsOwner(Guid crawlerId, string appKey);

    /// <summary>
    /// Executes work for a specific crawler under a per-crawler lock and optionally updates the stored crawler atomically.
    /// The provided function receives the current crawler (or null) and must return a tuple containing the result
    /// and an optional updated crawler instance. If the updated crawler is non-null it will replace the stored value.
    /// This helps implement safe modifications to crawler state.
    /// </summary>
    /// <typeparam name="T">Return type of the work function.</typeparam>
    /// <param name="id">Crawler identifier.</param>
    /// <param name="work">Function receiving current crawler and returning (result, updatedCrawler).</param>
    /// <returns>The result value returned by the work function.</returns>
    T RunLocked<T>(Guid id, Func<Crawler?, (T result, Crawler? updated)> work);
}

