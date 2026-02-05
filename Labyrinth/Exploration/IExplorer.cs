namespace Labyrinth.Exploration;

/// <summary>
/// Interface for explorers that can be orchestrated by MultiExplorer
/// Respects Interface Segregation Principle (ISP)
/// </summary>
public interface IExplorer
{
    /// <summary>
    /// Unique identifier for the explorer
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execute exploration asynchronously
    /// </summary>
    /// <param name="cancellationToken">Token to cancel exploration</param>
    Task ExploreAsync(CancellationToken cancellationToken = default);
}
