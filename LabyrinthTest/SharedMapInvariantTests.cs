using Labyrinth.Map;
using Labyrinth.Tiles;

namespace LabyrinthTest;

/// <summary>
/// Tests for SharedMap invariants (Issue #12)
/// Ensures data integrity under concurrent access
/// </summary>
[TestFixture]
public class SharedMapInvariantTests
{
    [Test]
    public void UpdateCell_FromKnownType_ToUnknown_IsRejected()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var position = (0, 0);
        var wall = Wall.Singleton;
        sharedMap.SetTileWithInvariant(position, wall);

        // Act
        var result = sharedMap.TrySetUnknown(position);

        // Assert
        Assert.That(result, Is.False, "Cannot revert known tile to unknown");
        Assert.That(sharedMap.GetTile(position), Is.EqualTo(wall));
        Assert.That(sharedMap.InvariantViolations, Has.Count.EqualTo(1));
        
        var firstViolation = sharedMap.InvariantViolations.First();
        Assert.That(firstViolation.Position, Is.EqualTo(position));
        Assert.That(firstViolation.Reason, Does.Contain("Unknown"));
    }

    [Test]
    public void UpdateCell_FromRoom_ToWall_LogsConflict()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var position = (5, 5);
        var room = new Room();
        sharedMap.SetTileWithInvariant(position, room);

        // Act
        var wall = Wall.Singleton;
        sharedMap.SetTileWithInvariant(position, wall);

        // Assert
        var tile = sharedMap.GetTile(position);
        Assert.That(tile, Is.Not.Null);
        Assert.That(sharedMap.ConflictLogs, Has.Count.GreaterThanOrEqualTo(1));
        
        var conflict = sharedMap.ConflictLogs.First();
        Assert.That(conflict.Position, Is.EqualTo(position));
        Assert.That(conflict.PreviousType, Does.Contain("Room"));
        Assert.That(conflict.NewType, Does.Contain("Wall"));
    }

    [Test]
    public void ConcurrentUpdates_SamePosition_LastWriteWins_WithLogging()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var position = (2, 2);
        
        // Act
        var task1 = Task.Run(() => sharedMap.SetTileWithInvariant(position, Wall.Singleton));
        var task2 = Task.Run(() => sharedMap.SetTileWithInvariant(position, new Room()));
        var task3 = Task.Run(() => sharedMap.SetTileWithInvariant(position, new Door()));
        
        Task.WaitAll(task1, task2, task3);

        // Assert
        var finalTile = sharedMap.GetTile(position);
        Assert.That(finalTile, Is.Not.Null);
        Assert.That(sharedMap.ConflictLogs, Is.Not.Empty, "Conflicts should be logged");
    }

    [Test]
    public void ConcurrentUpdates_DifferentPositions_AllSucceed()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var positions = Enumerable.Range(0, 100).Select(i => (i, i)).ToArray();
        
        // Act
        var tasks = positions.Select(pos => 
            Task.Run(() => sharedMap.SetTileWithInvariant(pos, new Room()))
        ).ToArray();
        Task.WaitAll(tasks);

        // Assert
        Assert.That(sharedMap.TileCount, Is.EqualTo(100));
        Assert.That(sharedMap.ConflictLogs, Is.Empty, "No conflicts on different positions");
    }

    [Test]
    public void KnownTileTypes_CanBeUpdatedToMoreSpecificType()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var position = (7, 7);
        var room = new Room();
        sharedMap.SetTileWithInvariant(position, room);

        // Act - Door is considered different type, will log conflict
        var door = new Door();
        sharedMap.SetTileWithInvariant(position, door);

        // Assert
        var tile = sharedMap.GetTile(position);
        Assert.That(tile, Is.InstanceOf<Door>());
        // Should log as conflict but still update
        Assert.That(sharedMap.ConflictLogs, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void StressTest_MassiveConcurrentWrites_NoDataCorruption()
    {
        // Arrange
        var sharedMap = new SharedMapWithInvariants();
        var writeCount = 1000;
        var position = (10, 10);
        
        // Act
        var tasks = Enumerable.Range(0, writeCount)
            .Select(_ => Task.Run(() => 
                sharedMap.SetTileWithInvariant(position, new Room())))
            .ToArray();
        Task.WaitAll(tasks);

        // Assert
        Assert.That(sharedMap.GetTile(position), Is.Not.Null);
        Assert.That(sharedMap.TileCount, Is.EqualTo(1), "Only one tile at this position");
        // Write attempts tracked
        Assert.That(sharedMap.WriteAttempts, Is.EqualTo(writeCount));
    }
}
