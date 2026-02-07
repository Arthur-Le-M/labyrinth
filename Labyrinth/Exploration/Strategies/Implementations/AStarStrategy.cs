using Labyrinth.Crawl;
using Labyrinth.Map;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration.Strategies.Implementations;

/// <summary>
/// A* pathfinding strategy for efficient navigation to a target position.
/// Uses Manhattan distance heuristic optimized for grid-based movement.
/// Follows SOLID principles:
/// - SRP: Focuses solely on pathfinding decisions
/// - OCP: Can be extended by overriding heuristic calculation
/// - LSP: Fully substitutable for IExplorationStrategy
/// - ISP: Implements only required strategy interface
/// - DIP: Depends on ISharedMap abstraction
/// </summary>
public class AStarStrategy : IExplorationStrategy
{
    private (int x, int y)? _target;
    private List<(int x, int y)>? _currentPath;
    private int _pathIndex;
    private (int x, int y)? _lastPosition;

    public string Name => "A*";

    public void SetTarget((int x, int y)? target)
    {
        _target = target;
        InvalidatePath();
    }

    public ExplorationAction DecideNextAction(ExplorationContext context)
    {
        if (_target == null)
            return ExplorationAction.Stop;

        var currentPos = context.CurrentPosition;

        if (currentPos == _target)
            return ExplorationAction.Stop;

        if (_lastPosition != null && _lastPosition != currentPos)
        {
            if (_currentPath != null && _pathIndex < _currentPath.Count && _currentPath[_pathIndex] == currentPos)
            {
                _pathIndex++;
            }
            else
            {
                InvalidatePath();
            }
        }
        _lastPosition = currentPos;

        if (_currentPath == null || _pathIndex >= _currentPath.Count)
        {
            _currentPath = CalculatePath(currentPos, _target.Value, context.KnownMap);
            _pathIndex = 0;

            if (_currentPath == null || _currentPath.Count == 0)
                return ExplorationAction.Stop;
        }

        var nextPos = _currentPath[_pathIndex];

        var (dx, dy) = (nextPos.x - currentPos.x, nextPos.y - currentPos.y);

        if (IsFacingDirection(context.CurrentDirection, dx, dy))
        {
            if (CanTraverse(context.FacingTileType, context.KnownMap, nextPos))
            {
                return ExplorationAction.Walk;
            }
            else
            {
                InvalidatePath();
                _currentPath = CalculatePath(currentPos, _target.Value, context.KnownMap);
                _pathIndex = 0;

                if (_currentPath == null || _currentPath.Count == 0)
                    return ExplorationAction.Stop;

                nextPos = _currentPath[_pathIndex];
                (dx, dy) = (nextPos.x - currentPos.x, nextPos.y - currentPos.y);

                if (IsFacingDirection(context.CurrentDirection, dx, dy))
                    return ExplorationAction.Stop;

                return TurnTowardsDirection(context.CurrentDirection, dx, dy);
            }
        }

        return TurnTowardsDirection(context.CurrentDirection, dx, dy);
    }

    /// <summary>
    /// Calculate the shortest path using A* algorithm.
    /// </summary>
    /// <param name="start">Starting position.</param>
    /// <param name="goal">Target position.</param>
    /// <param name="map">The known map for obstacle detection.</param>
    /// <returns>List of positions forming the path, or null if no path exists.</returns>
    private List<(int x, int y)>? CalculatePath((int x, int y) start, (int x, int y) goal, ISharedMap map)
    {
        var openSet = new PriorityQueue<(int x, int y), int>();
        var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();
        var gScore = new Dictionary<(int x, int y), int> { [start] = 0 };
        var fScore = new Dictionary<(int x, int y), int> { [start] = Heuristic(start, goal) };

        openSet.Enqueue(start, fScore[start]);
        var inOpenSet = new HashSet<(int x, int y)> { start };

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            inOpenSet.Remove(current);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!IsTraversable(neighbor, map))
                    continue;

                var tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);

                    if (!inOpenSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        inOpenSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Reconstruct path from A* search results.
    /// </summary>
    private static List<(int x, int y)> ReconstructPath(
        Dictionary<(int x, int y), (int x, int y)> cameFrom,
        (int x, int y) current)
    {
        var path = new List<(int x, int y)> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        // Remove the starting position (we're already there)
        if (path.Count > 0)
            path.RemoveAt(0);

        return path;
    }

    /// <summary>
    /// Get the four cardinal neighbors of a position.
    /// </summary>
    private static IEnumerable<(int x, int y)> GetNeighbors((int x, int y) pos)
    {
        yield return (pos.x, pos.y - 1); // North
        yield return (pos.x + 1, pos.y); // East
        yield return (pos.x, pos.y + 1); // South
        yield return (pos.x - 1, pos.y); // West
    }

    /// <summary>
    /// Check if a position is traversable based on map knowledge.
    /// Unknown positions are treated as potentially traversable (optimistic).
    /// </summary>
    private static bool IsTraversable((int x, int y) pos, ISharedMap map)
    {
        if (!map.IsKnown(pos))
            return true;

        var tile = map.GetTile(pos);
        if (tile == null)
            return true;

        if (tile is Wall || tile is Outside)
            return false;

        if (tile is Door door)
            return door.IsTraversable;

        return tile.IsTraversable;
    }

    /// <summary>
    /// Check if the crawler can traverse the tile it's facing.
    /// </summary>
    private static bool CanTraverse(Type facingTileType, ISharedMap map, (int x, int y) facingPos)
    {
        if (facingTileType == typeof(Wall) || facingTileType == typeof(Outside))
            return false;

        if (facingTileType == typeof(Door))
        {
            var tile = map.GetTile(facingPos);
            if (tile is Door door)
                return door.IsTraversable;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculate Manhattan distance heuristic.
    /// Admissible heuristic for grid-based pathfinding with 4-directional movement.
    /// </summary>
    protected virtual int Heuristic((int x, int y) a, (int x, int y) b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }

    /// <summary>
    /// Check if currently facing the given direction.
    /// </summary>
    private static bool IsFacingDirection(Direction current, int dx, int dy)
    {
        return current.DeltaX == dx && current.DeltaY == dy;
    }

    /// <summary>
    /// Determine the turn action needed to face a specific direction.
    /// </summary>
    private static ExplorationAction TurnTowardsDirection(Direction current, int dx, int dy)
    {
        var testRight = (Direction)current.Clone();
        testRight.TurnRight();

        if (testRight.DeltaX == dx && testRight.DeltaY == dy)
            return ExplorationAction.TurnRight;

        var testLeft = (Direction)current.Clone();
        testLeft.TurnLeft();

        if (testLeft.DeltaX == dx && testLeft.DeltaY == dy)
            return ExplorationAction.TurnLeft;

        return ExplorationAction.TurnRight;
    }

    /// <summary>
    /// Invalidate the current path, forcing recalculation on next decision.
    /// </summary>
    private void InvalidatePath()
    {
        _currentPath = null;
        _pathIndex = 0;
    }
}
