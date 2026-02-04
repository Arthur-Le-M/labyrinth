namespace LabyrinthApi.Services;

using ApiTypes;

/// <summary>
/// Service interface for managing crawler operations following the Interface Segregation Principle.
/// </summary>
public interface ICrawlerService
{
    /// <summary>
    /// Gets a crawler by its unique identifier.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>The crawler if found, null otherwise.</returns>
    Crawler? GetCrawler(Guid id);
    
    /// <summary>
    /// Checks if a crawler exists.
    /// </summary>
    /// <param name="id">The crawler's unique identifier.</param>
    /// <returns>True if the crawler exists, false otherwise.</returns>
    bool CrawlerExists(Guid id);
}

