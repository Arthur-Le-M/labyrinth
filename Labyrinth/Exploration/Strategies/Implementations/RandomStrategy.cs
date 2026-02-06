using Labyrinth.Sys;
using Labyrinth.Tiles;
using static Labyrinth.RandExplorer;
using Labyrinth.Exploration.Strategies;

namespace Labyrinth.Exploration.Strategies.Implementations;

/// <summary>
/// Original random exploration strategy extracted from RandExplorer.
/// Uses random decision-making to explore the labyrinth.
/// </summary>
public class RandomStrategy : IExplorationStrategy
{
    private readonly IEnumRandomizer<Actions> _randomizer;
    
    /// <inheritdoc />
    public string Name => "Random";
    
    /// <summary>
    /// Creates a new random exploration strategy.
    /// </summary>
    /// <param name="randomizer">Random generator for deciding actions.</param>
    public RandomStrategy(IEnumRandomizer<Actions> randomizer)
    {
        _randomizer = randomizer;
    }
    
    /// <inheritdoc />
    public ExplorationAction DecideNextAction(ExplorationContext context)
    {
        // Stop if we've reached the outside
        if (context.FacingTileType == typeof(Outside))
            return ExplorationAction.Stop;
        
        // If not facing a wall and random says walk, then walk
        if (context.FacingTileType != typeof(Wall) 
            && _randomizer.Next() == Actions.Walk)
        {
            return ExplorationAction.Walk;
        }
        
        // Default to turning left
        return ExplorationAction.TurnLeft;
    }
    
    /// <inheritdoc />
    public void SetTarget((int x, int y)? target)
    {
        // Ignored for random strategy - it doesn't use targets
    }
}
