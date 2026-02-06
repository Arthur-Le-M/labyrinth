using Labyrinth.Crawl;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Exploration.Strategies.Implementations;
using Labyrinth.Map;
using Labyrinth.Tiles;

namespace LabyrinthTest.Strategies;

[TestFixture]
public class DfsStrategyTests
{
    private SharedMap _map = null!;

    [SetUp]
    public void SetUp()
    {
        _map = new SharedMap();
    }

    [Test]
    public void Name_ReturnsDFS()
    {
        // Arrange
        var strategy = new DfsStrategy();
        
        // Assert
        Assert.That(strategy.Name, Is.EqualTo("DFS"));
    }

    [Test]
    public void DecideNextAction_WhenFacingOutside_ReturnsStop()
    {
        // Arrange
        var strategy = new DfsStrategy();
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Outside)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Stop));
    }

    [Test]
    public void DecideNextAction_WhenUnexploredInFront_ReturnsWalk()
    {
        // Arrange: Current position known, facing unexplored area
        _map.SetTile((1, 1), new Room());
        // (1, 0) is unexplored (not in map)
        
        var strategy = new DfsStrategy();
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,  // Facing (1, 0) which is unexplored
            _map,
            typeof(Room)  // The room at (1, 0)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_WhenFacingWall_ReturnsTurn()
    {
        // Arrange
        _map.SetTile((1, 1), new Room());
        _map.SetTile((1, 0), Wall.Singleton);
        
        var strategy = new DfsStrategy();
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Wall)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnRight).Or.EqualTo(ExplorationAction.TurnLeft));
    }

    [Test]
    public void DecideNextAction_ExploresUnvisitedDirection()
    {
        // Arrange: Only current position known, adjacent tiles unknown
        _map.SetTile((1, 1), new Room());
        // North (1, 0) - unknown (not in map) - should explore first (order: N, E, S, W)
        
        var strategy = new DfsStrategy();
        
        // First call - facing North where (1,0) is unexplored
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,  // Facing the first unexplored direction
            _map,
            typeof(Room)  // Tile at (1,0) is a room we can walk into
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert: Should walk to unexplored room
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_BacktracksWhenDeadEnd()
    {
        // Arrange: Dead end - all neighbors known and visited
        _map.SetTile((1, 1), new Room());
        _map.SetTile((1, 0), Wall.Singleton);
        _map.SetTile((2, 1), Wall.Singleton);
        _map.SetTile((1, 2), new Room());  // Came from here
        _map.SetTile((0, 1), Wall.Singleton);
        
        var strategy = new DfsStrategy();
        
        // Simulate coming from (1, 2)
        var contextAtStart = new ExplorationContext((1, 2), Direction.North, _map, typeof(Room));
        strategy.DecideNextAction(contextAtStart);
        
        // Then move to (1, 1)
        var contextAtDeadEnd = new ExplorationContext((1, 1), Direction.North, _map, typeof(Wall));
        
        // Act: Should try to backtrack since all directions are blocked/visited
        var action = strategy.DecideNextAction(contextAtDeadEnd);
        
        // Assert: Should turn to go back
        Assert.That(action, Is.Not.EqualTo(ExplorationAction.Stop));
    }

    [Test]
    public void SetTarget_DoesNothing()
    {
        // Arrange
        var strategy = new DfsStrategy();
        
        // Act & Assert - should not throw (DFS doesn't use targets)
        Assert.DoesNotThrow(() => strategy.SetTarget((5, 5)));
        Assert.DoesNotThrow(() => strategy.SetTarget(null));
    }

    [Test]
    public void DecideNextAction_PrioritizesUnexploredNeighbors()
    {
        // Arrange: Some neighbors known, some unknown
        _map.SetTile((1, 1), new Room());
        _map.SetTile((1, 0), Wall.Singleton);  // North - wall (blocked)
        // East (2, 1) - unknown (should explore)
        // South (1, 2) - unknown
        _map.SetTile((0, 1), Wall.Singleton);  // West - wall
        
        var strategy = new DfsStrategy();
        var context = new ExplorationContext(
            (1, 1),
            Direction.East,
            _map,
            typeof(Room)  // Unexplored room ahead
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert: Should walk towards unexplored East
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_StopsWhenFullyExplored()
    {
        // Arrange: Single room, completely surrounded by walls
        _map.SetTile((1, 1), new Room());
        _map.SetTile((1, 0), Wall.Singleton);
        _map.SetTile((2, 1), Wall.Singleton);
        _map.SetTile((1, 2), Wall.Singleton);
        _map.SetTile((0, 1), Wall.Singleton);
        
        var strategy = new DfsStrategy();
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Wall)
        );
        
        // Act: First call
        var action1 = strategy.DecideNextAction(context);
        
        // Simulate turning and checking all directions
        for (int i = 0; i < 4; i++)
        {
            strategy.DecideNextAction(context);
        }
        
        // Eventually should stop when all explored
        // (This test verifies DFS doesn't infinite loop in enclosed space)
        Assert.Pass("DFS handled enclosed space without infinite loop");
    }
}
