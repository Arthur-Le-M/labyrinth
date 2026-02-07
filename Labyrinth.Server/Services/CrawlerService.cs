namespace Labyrinth.Server.Services;

using ApiTypes;
using System.Collections.Concurrent;

/// <summary>
/// In-memory implementation of the crawler service for managing crawler state.
/// </summary>
public class CrawlerService : ICrawlerService
{
    private readonly ConcurrentDictionary<Guid, Crawler> _crawlers = new();
    private readonly ConcurrentDictionary<Guid, string> _crawlerAppKeys = new();
    // Per-crawler lock objects to serialize updates for a single crawler
    private readonly ConcurrentDictionary<Guid, object> _crawlerLocks = new();

    /// <summary>
    /// Adds a new crawler to the service.
    /// </summary>
    public void AddCrawler(Crawler crawler)
    {
        _crawlers[crawler.Id] = crawler;
        _crawlerLocks.GetOrAdd(crawler.Id, _ => new object());
    }

    /// <inheritdoc />
    public void AddCrawler(Crawler crawler, string appKey)
    {
        _crawlers[crawler.Id] = crawler;
        _crawlerAppKeys[crawler.Id] = appKey;
        _crawlerLocks.GetOrAdd(crawler.Id, _ => new object());
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
            .Select(kvp => _crawlers.TryGetValue(kvp.Key, out var c) ? c : null)
            .Where(c => c != null)!
            .ToArray()!;
    }

    /// <inheritdoc />
    public bool CrawlerExists(Guid id)
    {
        return _crawlers.ContainsKey(id);
    }

    /// <summary>
    /// Updates an existing crawler's state only if it already exists.
    /// </summary>
    public void UpdateCrawler(Crawler crawler)
    {
        if (_crawlers.TryGetValue(crawler.Id, out var existing))
        {
            // TryUpdate uses a compare-exchange semantics to avoid races
            _crawlers.TryUpdate(crawler.Id, crawler, existing);
        }
    }

    /// <summary>
    /// Deletes a crawler by its unique identifier.
    /// </summary>
    public bool DeleteCrawler(Guid id)
    {
        _crawlerAppKeys.TryRemove(id, out _);
        _crawlerLocks.TryRemove(id, out _);
        return _crawlers.TryRemove(id, out _);
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

    private object GetLock(Guid id) => _crawlerLocks.GetOrAdd(id, _ => new object());

    /// <inheritdoc />
    public T RunLocked<T>(Guid id, Func<Crawler?, (T result, Crawler? updated)> work)
    {
        var l = GetLock(id);
        lock (l)
        {
            _crawlers.TryGetValue(id, out var current);
            var (result, updated) = work(current);
            if (updated != null)
            {
                _crawlers[id] = updated;
            }
            return result;
        }
    }
}
