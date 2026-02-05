namespace Labyrinth.Exploration;

/// <summary>
/// Optional interface for explorers that track execution steps
/// Respects Interface Segregation Principle - not all explorers need to implement this
/// </summary>
public interface IStepsTrackingExplorer : IExplorer
{
    /// <summary>
    /// Number of steps executed by this explorer
    /// </summary>
    int StepsExecuted { get; }
}
