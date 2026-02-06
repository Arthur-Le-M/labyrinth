using Labyrinth.Crawl;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Map;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration.Strategies.Implementations;

/// <summary>
/// DFS strategy for systematic exploration of unknown areas.
/// Uses Depth-First Search with backtracking.
/// Prioritizes unexplored tiles and backtracks when reaching dead ends.
/// </summary>
public class DfsStrategy : IExplorationStrategy
{
    private readonly Stack<(int x, int y)> _visitedStack = new();
    private readonly HashSet<(int x, int y)> _fullyExplored = new();
    
    /// <inheritdoc />
    public string Name => "DFS";
    
    /// <inheritdoc />
    public void SetTarget((int x, int y)? target)
    {
        // DFS explores everything systematically, doesn't use targets
    }
    
    /// <inheritdoc />
    public ExplorationAction DecideNextAction(ExplorationContext context)
    {
        var currentPos = context.CurrentPosition;
        
        // Check if outside reached
        if (context.FacingTileType == typeof(Outside))
            return ExplorationAction.Stop;
        
        // Mark current position as visited
        if (_visitedStack.Count == 0 || _visitedStack.Peek() != currentPos)
        {
            _visitedStack.Push(currentPos);
        }
        
        // Priority 1: Find an unexplored neighbor
        var unexploredDir = FindUnexploredDirection(context);
        
        if (unexploredDir != null)
        {
            var (dx, dy) = unexploredDir.Value;
            
            // Check if we're facing the unexplored direction
            if (IsFacingDirection(context.CurrentDirection, dx, dy))
            {
                // Can walk if not facing a wall, outside, or closed door
                if (context.FacingTileType != typeof(Wall) && 
                    context.FacingTileType != typeof(Outside) &&
                    context.FacingTileType != typeof(Door)) // Door is closed if we can't walk through
                    return ExplorationAction.Walk;
                
                // Check if it's a door - might be traversable if open
                if (context.FacingTileType == typeof(Door))
                {
                    var facingPos = (currentPos.x + dx, currentPos.y + dy);
                    var tile = context.KnownMap.GetTile(facingPos);
                    if (tile != null && tile.IsTraversable)
                        return ExplorationAction.Walk;
                    
                    // Door is closed - don't mark as fully explored, just find another direction
                    return TurnTowardsUnexplored(context, unexploredDir.Value, markAsExplored: false);
                }
                    
                // Wall or outside ahead - mark and find another direction
                return TurnTowardsUnexplored(context, unexploredDir.Value, markAsExplored: true);
            }
            
            // Turn towards unexplored direction
            return TurnTowardsDirection(context.CurrentDirection, dx, dy);
        }
        
        // Priority 2: Backtrack if all neighbors explored
        if (_visitedStack.Count > 1)
        {
            _fullyExplored.Add(currentPos);
            _visitedStack.Pop();
            
            var previousPos = _visitedStack.Peek();
            return MoveTowards(context, previousPos);
        }
        
        // Fully explored - nowhere to go
        return ExplorationAction.Stop;
    }
    
    /// <summary>
    /// Find a direction with an unexplored tile that could be traversable.
    /// </summary>
    private (int dx, int dy)? FindUnexploredDirection(ExplorationContext context)
    {
        // N, E, S, W
        var deltas = new[] { (0, -1), (1, 0), (0, 1), (-1, 0) };
        var pos = context.CurrentPosition;
        
        foreach (var (dx, dy) in deltas)
        {
            var neighbor = (pos.x + dx, pos.y + dy);
            
            // Skip if already fully explored
            if (_fullyExplored.Contains(neighbor))
                continue;
            
            // Skip if already in visited stack (currently being explored - prevents loops)
            if (_visitedStack.Contains(neighbor))
                continue;
            
            // Check if known in map
            if (context.KnownMap.IsKnown(neighbor))
            {
                var tile = context.KnownMap.GetTile(neighbor);
                // Skip walls and outside - they're not explorable
                // But doors might be openable later, so don't skip them
                if (tile is Wall || tile is Outside)
                    continue;
            }
            
            // This direction has an unexplored or traversable tile
            return (dx, dy);
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if currently facing the given direction.
    /// </summary>
    private static bool IsFacingDirection(Direction current, int dx, int dy)
    {
        return current.DeltaX == dx && current.DeltaY == dy;
    }
    
    /// <summary>
    /// Turn towards a specific direction.
    /// </summary>
    private static ExplorationAction TurnTowardsDirection(Direction current, int dx, int dy)
    {
        // Check if turning right gets us there
        var testRight = (Direction)current.Clone();
        testRight.TurnRight();
        
        if (testRight.DeltaX == dx && testRight.DeltaY == dy)
            return ExplorationAction.TurnRight;
        
        // Otherwise turn left
        return ExplorationAction.TurnLeft;
    }
    
    /// <summary>
    /// Turn towards unexplored when current direction is blocked.
    /// </summary>
    /// <param name="context">Current exploration context.</param>
    /// <param name="blockedDir">The direction that is blocked.</param>
    /// <param name="markAsExplored">Whether to mark the blocked position as fully explored (false for doors).</param>
    private ExplorationAction TurnTowardsUnexplored(ExplorationContext context, (int dx, int dy) blockedDir, bool markAsExplored)
    {
        var pos = context.CurrentPosition;
        var blockedPos = (pos.x + blockedDir.dx, pos.y + blockedDir.dy);
        
        // Only mark as fully explored if it's a wall/outside, not a closed door
        if (markAsExplored)
        {
            _fullyExplored.Add(blockedPos);
        }
        
        // Find another unexplored direction
        var deltas = new[] { (0, -1), (1, 0), (0, 1), (-1, 0) };
        
        foreach (var (dx, dy) in deltas)
        {
            if ((dx, dy) == blockedDir) continue;
            
            var neighbor = (pos.x + dx, pos.y + dy);
            
            if (!context.KnownMap.IsKnown(neighbor) && !_fullyExplored.Contains(neighbor))
            {
                return TurnTowardsDirection(context.CurrentDirection, dx, dy);
            }
        }
        
        // No unexplored direction - just turn
        return ExplorationAction.TurnLeft;
    }
    
    /// <summary>
    /// Move towards a specific position (for backtracking).
    /// </summary>
    private static ExplorationAction MoveTowards(ExplorationContext context, (int x, int y) targetPos)
    {
        var (x, y) = context.CurrentPosition;
        var dir = context.CurrentDirection;
        
        var dx = targetPos.x - x;
        var dy = targetPos.y - y;
        
        // Normalize to unit direction
        if (dx != 0) dx = dx / Math.Abs(dx);
        if (dy != 0) dy = dy / Math.Abs(dy);
        
        // If facing the right direction, walk
        if (dir.DeltaX == dx && dir.DeltaY == dy)
        {
            return ExplorationAction.Walk;
        }
        
        // Turn towards target
        return TurnTowardsDirection(dir, dx, dy);
    }
}
