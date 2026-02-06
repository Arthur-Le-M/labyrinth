namespace Labyrinth.Exploration.Strategies;

/// <summary>
/// Strategy pattern for exploration algorithms.
/// Each strategy decides the next action based on current state.
/// </summary>
public interface IExplorationStrategy
{
    /// <summary>
    /// Name of the strategy for logging/CLI selection.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Decide the next action based on current crawler state and knowledge.
    /// </summary>
    /// <param name="context">Current exploration context (position, direction, map knowledge).</param>
    /// <returns>The next action to take.</returns>
    ExplorationAction DecideNextAction(ExplorationContext context);
    
    /// <summary>
    /// Optional: Set a target position for pathfinding strategies.
    /// </summary>
    /// <param name="target">Target position or null to clear the target.</param>
    void SetTarget((int x, int y)? target);
}

