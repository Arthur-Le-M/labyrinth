namespace Labyrinth.Exploration.Strategies;

/// <summary>
/// Actions that a strategy can decide.
/// </summary>
public enum ExplorationAction
{
    /// <summary>
    /// Move forward in the current direction.
    /// </summary>
    Walk,
    
    /// <summary>
    /// Turn 90 degrees to the left (counter-clockwise).
    /// </summary>
    TurnLeft,
    
    /// <summary>
    /// Turn 90 degrees to the right (clockwise).
    /// </summary>
    TurnRight,
    
    /// <summary>
    /// Stop exploration (reached goal or no path found).
    /// </summary>
    Stop
}

