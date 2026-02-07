using Labyrinth.Crawl;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Exploration.Strategies.Implementations;
using Labyrinth.Map;
using Labyrinth.Tiles;

namespace LabyrinthTest.Strategies;

/// <summary>
/// Unit tests for AStarStrategy using TDD approach with AAA pattern.
/// Tests cover pathfinding behavior, edge cases, and SOLID compliance.
/// </summary>
[TestFixture]
public class AStarStrategyTests
{
    private SharedMap _map = null!;

    [SetUp]
    public void SetUp()
    {
        _map = new SharedMap();
    }

    #region Name Tests

    [Test]
    public void Name_ReturnsAStar()
    {
        // Arrange
        var strategy = new AStarStrategy();

        // Act
        var name = strategy.Name;

        // Assert
        Assert.That(name, Is.EqualTo("A*"));
    }

    #endregion

    #region SetTarget Tests

    [Test]
    public void SetTarget_WithValidTarget_DoesNotThrow()
    {
        // Arrange
        var strategy = new AStarStrategy();

        // Act & Assert
        Assert.DoesNotThrow(() => strategy.SetTarget((5, 5)));
    }

    [Test]
    public void SetTarget_WithNull_DoesNotThrow()
    {
        // Arrange
        var strategy = new AStarStrategy();

        // Act & Assert
        Assert.DoesNotThrow(() => strategy.SetTarget(null));
    }

    #endregion

    #region DecideNextAction - No Target Tests

    [Test]
    public void DecideNextAction_WithNoTarget_ReturnsStop()
    {
        // Arrange
        _map.SetTile((0, 0), new Room());
        var strategy = new AStarStrategy();
        // No target set
        var context = new ExplorationContext(
            (0, 0),
            Direction.North,
            _map,
            typeof(Room)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Stop));
    }

    #endregion

    #region DecideNextAction - Target Reached Tests

    [Test]
    public void DecideNextAction_WhenAtTarget_ReturnsStop()
    {
        // Arrange
        _map.SetTile((3, 3), new Room());
        var strategy = new AStarStrategy();
        strategy.SetTarget((3, 3));
        
        var context = new ExplorationContext(
            (3, 3),  // Already at target
            Direction.North,
            _map,
            typeof(Room),
            Target: (3, 3)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Stop));
    }

    #endregion

    #region DecideNextAction - Simple Path Tests

    [Test]
    public void DecideNextAction_TargetDirectlyNorth_FacingNorth_ReturnsWalk()
    {
        // Arrange: Straight line path North
        _map.SetTile((0, 1), new Room());  // Current position
        _map.SetTile((0, 0), new Room());  // Target (directly North)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((0, 0));
        
        var context = new ExplorationContext(
            (0, 1),
            Direction.North,  // Already facing the target
            _map,
            typeof(Room),
            Target: (0, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_TargetDirectlyEast_FacingNorth_ReturnsTurnRight()
    {
        // Arrange: Target is East, but facing North
        _map.SetTile((0, 0), new Room());  // Current position
        _map.SetTile((1, 0), new Room());  // Target (directly East)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((1, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.North,  // Need to turn right to face East
            _map,
            typeof(Room),
            Target: (1, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnRight));
    }

    [Test]
    public void DecideNextAction_TargetDirectlyWest_FacingNorth_ReturnsTurnLeft()
    {
        // Arrange: Target is West, but facing North
        _map.SetTile((1, 0), new Room());  // Current position
        _map.SetTile((0, 0), new Room());  // Target (directly West)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((0, 0));
        
        var context = new ExplorationContext(
            (1, 0),
            Direction.North,  // Need to turn left to face West
            _map,
            typeof(Room),
            Target: (0, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft));
    }

    [Test]
    public void DecideNextAction_TargetDirectlySouth_FacingNorth_ReturnsTurn()
    {
        // Arrange: Target is South, but facing North (need to turn around)
        _map.SetTile((0, 0), new Room());  // Current position
        _map.SetTile((0, 1), new Room());  // Target (directly South)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((0, 1));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.North,  // Need to turn around to face South
            _map,
            typeof(Room),
            Target: (0, 1)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should turn (either direction is valid for 180°)
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft).Or.EqualTo(ExplorationAction.TurnRight));
    }

    #endregion

    #region DecideNextAction - Obstacle Avoidance Tests

    [Test]
    public void DecideNextAction_WallBlocking_FindsAlternativePath()
    {
        // Arrange: Wall between current position and target
        //   0 1 2
        // 0 S # T   (S=Start, #=Wall, T=Target)
        // 1 . . .   (rooms to go around)
        _map.SetTile((0, 0), new Room());  // Start
        _map.SetTile((1, 0), Wall.Singleton);  // Wall blocking direct path
        _map.SetTile((2, 0), new Room());  // Target
        _map.SetTile((0, 1), new Room());  // Alternative path
        _map.SetTile((1, 1), new Room());
        _map.SetTile((2, 1), new Room());
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,  // Facing the wall
            _map,
            typeof(Wall),  // Facing a wall
            Target: (2, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should not walk into wall, should turn to find path around
        Assert.That(action, Is.Not.EqualTo(ExplorationAction.Walk));
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft).Or.EqualTo(ExplorationAction.TurnRight));
    }

    [Test]
    public void DecideNextAction_FacingWall_ReturnsTurn()
    {
        // Arrange
        _map.SetTile((0, 0), new Room());
        _map.SetTile((0, -1), Wall.Singleton);  // Wall to the North
        _map.SetTile((1, 0), new Room());  // Path to go around
        _map.SetTile((1, -1), new Room());  // Target
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((1, -1));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.North,  // Facing the wall
            _map,
            typeof(Wall),
            Target: (1, -1)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should turn to avoid wall
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft).Or.EqualTo(ExplorationAction.TurnRight));
    }

    #endregion

    #region DecideNextAction - Complex Path Tests

    [Test]
    public void DecideNextAction_LShapedPath_NavigatesCorrectly()
    {
        // Arrange: L-shaped corridor
        //   0 1 2
        // 0 S # #
        // 1 . # #
        // 2 . . T
        _map.SetTile((0, 0), new Room());  // Start
        _map.SetTile((1, 0), Wall.Singleton);
        _map.SetTile((2, 0), Wall.Singleton);
        _map.SetTile((0, 1), new Room());
        _map.SetTile((1, 1), Wall.Singleton);
        _map.SetTile((2, 1), Wall.Singleton);
        _map.SetTile((0, 2), new Room());
        _map.SetTile((1, 2), new Room());
        _map.SetTile((2, 2), new Room());  // Target
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 2));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.South,
            _map,
            typeof(Room),
            Target: (2, 2)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should walk South (first step of the path)
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_MultiplePaths_ChoosesShortest()
    {
        // Arrange: Two possible paths, one shorter
        //   0 1 2 3
        // 0 S . . T   (short path: 3 steps)
        // 1 . # # .
        // 2 . . . .   (long path: 6 steps)
        _map.SetTile((0, 0), new Room());  // Start
        _map.SetTile((1, 0), new Room());
        _map.SetTile((2, 0), new Room());
        _map.SetTile((3, 0), new Room());  // Target
        _map.SetTile((0, 1), new Room());
        _map.SetTile((1, 1), Wall.Singleton);
        _map.SetTile((2, 1), Wall.Singleton);
        _map.SetTile((3, 1), new Room());
        _map.SetTile((0, 2), new Room());
        _map.SetTile((1, 2), new Room());
        _map.SetTile((2, 2), new Room());
        _map.SetTile((3, 2), new Room());
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((3, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,  // Already facing the short path
            _map,
            typeof(Room),
            Target: (3, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should walk East (shortest path)
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    #endregion

    #region DecideNextAction - No Path Tests

    [Test]
    public void DecideNextAction_NoPathExists_FullyKnownMap_ReturnsStop()
    {
        // Arrange: Target completely surrounded by walls - ALL tiles known
        // This tests the pessimistic scenario where the map is fully explored
        //   0 1 2 3 4
        // 0 # # # # #
        // 1 # S . # #
        // 2 # . . # #
        // 3 # # # T #   (T is unreachable - surrounded by walls)
        // 4 # # # # #
        
        // Fill entire area with walls first
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                _map.SetTile((x, y), Wall.Singleton);
        
        // Carve out the accessible area
        _map.SetTile((1, 1), new Room());  // Start
        _map.SetTile((2, 1), new Room());
        _map.SetTile((1, 2), new Room());
        _map.SetTile((2, 2), new Room());
        _map.SetTile((3, 3), new Room());  // Target (unreachable - surrounded by walls)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((3, 3));
        
        var context = new ExplorationContext(
            (1, 1),
            Direction.East,
            _map,
            typeof(Room),
            Target: (3, 3)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: No path exists through known tiles, should stop
        Assert.That(action, Is.EqualTo(ExplorationAction.Stop));
    }

    [Test]
    public void DecideNextAction_UnknownTilesAroundTarget_AttemptsPath()
    {
        // Arrange: Target might be reachable through unknown tiles (optimistic)
        // A* treats unknown tiles as potentially traversable
        _map.SetTile((0, 0), new Room());  // Start - only known tile
        // Target at (2, 0) - unknown path
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Room),  // Assume facing tile is traversable
            Target: (2, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should attempt to walk towards target through unknown tiles
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    #endregion

    #region DecideNextAction - Unknown Tiles Tests

    [Test]
    public void DecideNextAction_UnknownTilesOnPath_TreatsAsTraversable()
    {
        // Arrange: Path through unknown territory
        _map.SetTile((0, 0), new Room());  // Start - only known tile
        // (1, 0), (2, 0) are unknown
        // Target at (2, 0)
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Room),  // Assume facing tile is traversable (for exploration)
            Target: (2, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should attempt to walk towards unknown (optimistic pathfinding)
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    #endregion

    #region DecideNextAction - Door Handling Tests

    [Test]
    public void DecideNextAction_TraversableDoorOnPath_WalksThroughDoor()
    {
        // Arrange: Path through a traversable door
        // Note: Door is created with a key in it, making it open/traversable by default
        _map.SetTile((0, 0), new Room());
        _map.SetTile((1, 0), new Door());  // Door is open by default (has key)
        _map.SetTile((2, 0), new Room());  // Target
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Door),  // Facing a door (open)
            Target: (2, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should walk through the open door
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_NonTraversableTileOnPath_FindsAlternativeRoute()
    {
        // Arrange: Wall blocking the direct path, must go around
        _map.SetTile((0, 0), new Room());
        _map.SetTile((1, 0), Wall.Singleton);  // Wall blocking direct path
        _map.SetTile((2, 0), new Room());  // Target
        _map.SetTile((0, 1), new Room());  // Alternative path
        _map.SetTile((1, 1), new Room());
        _map.SetTile((2, 1), new Room());
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Wall),  // Facing wall
            Target: (2, 0)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should turn to find alternative path around the wall
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft).Or.EqualTo(ExplorationAction.TurnRight));
    }

    #endregion

    #region Recalculation Tests

    [Test]
    public void DecideNextAction_PathChangesAfterMove_RecalculatesPath()
    {
        // Arrange: Initial path exists
        _map.SetTile((0, 0), new Room());
        _map.SetTile((1, 0), new Room());
        _map.SetTile((2, 0), new Room());  // Target
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 0));
        
        // First decision at (0,0)
        var context1 = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Room),
            Target: (2, 0)
        );
        var action1 = strategy.DecideNextAction(context1);
        Assert.That(action1, Is.EqualTo(ExplorationAction.Walk));
        
        // Simulate moving to (1, 0) and discovering a wall appeared
        _map.SetTile((2, 0), Wall.Singleton);  // Target is now a wall!
        _map.SetTile((2, 1), new Room());  // New target
        strategy.SetTarget((2, 1));  // Change target
        
        // Second decision at (1,0)
        var context2 = new ExplorationContext(
            (1, 0),
            Direction.East,
            _map,
            typeof(Wall),  // Now facing wall
            Target: (2, 1)
        );

        // Act
        var action2 = strategy.DecideNextAction(context2);

        // Assert: Should recalculate and turn towards new path
        Assert.That(action2, Is.Not.EqualTo(ExplorationAction.Walk));
    }

    #endregion

    #region Heuristic Tests

    [Test]
    public void DecideNextAction_DiagonalTarget_UsesManhattanDistance()
    {
        // Arrange: Target is diagonal - A* should use Manhattan distance heuristic
        //   0 1 2
        // 0 S . .
        // 1 . . .
        // 2 . . T
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                _map.SetTile((x, y), new Room());
        
        var strategy = new AStarStrategy();
        strategy.SetTarget((2, 2));
        
        var context = new ExplorationContext(
            (0, 0),
            Direction.East,
            _map,
            typeof(Room),
            Target: (2, 2)
        );

        // Act
        var action = strategy.DecideNextAction(context);

        // Assert: Should move towards target (East or South are both valid first steps)
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk).Or.EqualTo(ExplorationAction.TurnRight));
    }

    #endregion
}
