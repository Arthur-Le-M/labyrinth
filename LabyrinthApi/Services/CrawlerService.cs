namespace LabyrinthApi.Services;

using ApiTypes;

/// <summary>
/// In-memory implementation of the crawler service for managing crawler state.
/// </summary>
public class CrawlerService : ICrawlerService
{
    private readonly Dictionary<Guid, Crawler> _crawlers = new();
    
    /// <summary>
    /// Adds a new crawler to the service.
    /// </summary>
    /// <param name="crawler">The crawler to add.</param>
    public void AddCrawler(Crawler crawler)
    {
        _crawlers[crawler.Id] = crawler;
    }
    
    /// <inheritdoc />
    public Crawler? GetCrawler(Guid id)
    {
        return _crawlers.TryGetValue(id, out var crawler) ? crawler : null;
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
}
