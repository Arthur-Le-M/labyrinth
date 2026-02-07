namespace Labyrinth.Server.Services;

using ApiTypes;

/// <summary>
/// In-memory implementation of the crawler service for managing crawler state.
/// </summary>
public class CrawlerService : ICrawlerService
{
    private readonly Dictionary<Guid, Crawler> _crawlers = new();
    private readonly Dictionary<Guid, string> _crawlerAppKeys = new();
    
    /// <summary>
    /// Adds a new crawler to the service.
    /// </summary>
    /// <param name="crawler">The crawler to add.</param>
    public void AddCrawler(Crawler crawler)
    {
        _crawlers[crawler.Id] = crawler;
    }
    
    /// <inheritdoc />
    public void AddCrawler(Crawler crawler, string appKey)
    {
        _crawlers[crawler.Id] = crawler;
        _crawlerAppKeys[crawler.Id] = appKey;
    }
    
    /// <inheritdoc />
    public Crawler? GetCrawler(Guid id)
    {
        return _crawlers.TryGetValue(id, out var crawler) ? crawler : null;
    }
    
    /// <inheritdoc />
    public IEnumerable<Crawler> GetCrawlersByAppKey(string appKey)
    {
        return _crawlerAppKeys
            .Where(kvp => kvp.Value == appKey)
            .Select(kvp => _crawlers[kvp.Key])
            .ToArray();
    }
    
    /// <inheritdoc />
    public bool CrawlerExists(Guid id)
    {
        return _crawlers.ContainsKey(id);
    }
    
    /// <summary>
    /// Updates an existing crawler's state.
    /// </summary>
    /// <param name="crawler">The crawler with updated state.</param>
    public void UpdateCrawler(Crawler crawler)
    {
        if (_crawlers.ContainsKey(crawler.Id))
        {
            _crawlers[crawler.Id] = crawler;
        }
    }
    
    /// <summary>
    /// Deletes a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>True if the crawler was deleted, false if it didn't exist.</returns>
    public bool DeleteCrawler(Guid id)
    {
        _crawlerAppKeys.Remove(id);
        return _crawlers.Remove(id);
    }

    /// <inheritdoc />
    public int GetCrawlerCountByAppKey(string appKey)
    {
        return _crawlerAppKeys.Count(kvp => kvp.Value == appKey);
    }

    /// <inheritdoc />
    public string? GetAppKeyForCrawler(Guid crawlerId)
    {
        return _crawlerAppKeys.TryGetValue(crawlerId, out var appKey) ? appKey : null;
    }

    /// <inheritdoc />
    public bool IsOwner(Guid crawlerId, string appKey)
    {
        return _crawlerAppKeys.TryGetValue(crawlerId, out var storedAppKey) 
               && storedAppKey == appKey;
    }
}
