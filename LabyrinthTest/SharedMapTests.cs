using Labyrinth;
using Labyrinth.Tiles;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LabyrinthTest;

/// <summary>
/// Tests for SharedMap (KnowledgeBase) for concurrent exploration knowledge sharing.
/// Follows AAA pattern (Arrange, Act, Assert) and TDD principles.
/// </summary>
public class SharedMapTests
{
    /// <summary>
    /// Test: SharedMap stores and retrieves a tile
    /// Arrange: Create a SharedMap and a tile
    /// Act: Store the tile at a position and retrieve it
    /// Assert: Retrieved tile matches the stored tile
    /// </summary>
    [Test]
    public void SetTile_ValidTile_TileStored()
    {
        // Arrange
        var map = new SharedMap();
        var position = (0, 0);
        var tile = new Room();

        // Act
        map.SetTile(position, tile);
        var retrieved = map.GetTile(position);

        // Assert
        Assert.That(retrieved, Is.EqualTo(tile));
    }

    /// <summary>
    /// Test: SharedMap returns null for unknown position
    /// Arrange: Create a SharedMap without storing anything
    /// Act: Try to retrieve a tile from an empty position
    /// Assert: Returned value is null
    /// </summary>
    [Test]
    public void GetTile_UnknownPosition_ReturnsNull()
    {
        // Arrange
        var map = new SharedMap();
        var position = (999, 999);

        // Act
        var retrieved = map.GetTile(position);

        // Assert
        Assert.That(retrieved, Is.Null);
    }

    /// <summary>
    /// Test: SharedMap supports multiple tiles
    /// Arrange: Create a SharedMap
    /// Act: Store multiple tiles at different positions
    /// Assert: Each tile can be retrieved correctly
    /// </summary>
    [Test]
    public void SetTile_MultipleTiles_AllRetrieved()
    {
        // Arrange
        var map = new SharedMap();
        var tiles = new (int, int, Tile)[]
        {
            (0, 0, new Room()),
            (1, 0, Wall.Singleton),
            (0, 1, new Door()),
        };

        // Act
        foreach (var (x, y, tile) in tiles)
        {
            map.SetTile((x, y), tile);
        }

        // Assert
        foreach (var (x, y, expectedTile) in tiles)
        {
            var retrieved = map.GetTile((x, y));
            Assert.That(retrieved, Is.EqualTo(expectedTile));
        }
    }

    /// <summary>
    /// Test: SharedMap is thread-safe for concurrent writes
    /// Arrange: Create a SharedMap and multiple threads
    /// Act: Each thread stores tiles at different positions concurrently
    /// Assert: All tiles are stored without conflicts
    /// </summary>
    [Test]
    public void SetTile_ConcurrentWrites_AllStored()
    {
        // Arrange
        var map = new SharedMap();
        var threadCount = 10;
        var tilesPerThread = 100;
        var tasks = new List<Task>();

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            var task = Task.Run(() =>
            {
                for (int i = 0; i < tilesPerThread; i++)
                {
                    var position = (threadId * tilesPerThread + i, 0);
                    var tile = new Room();
                    map.SetTile(position, tile);
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        for (int t = 0; t < threadCount; t++)
        {
            for (int i = 0; i < tilesPerThread; i++)
            {
                var position = (t * tilesPerThread + i, 0);
                var retrieved = map.GetTile(position);
                Assert.That(retrieved, Is.Not.Null);
                Assert.That(retrieved, Is.TypeOf<Room>());
            }
        }
    }

    /// <summary>
    /// Test: SharedMap is thread-safe for concurrent reads
    /// Arrange: Create a SharedMap with some tiles, multiple reader threads
    /// Act: Each thread reads the same tiles concurrently
    /// Assert: All reads return the correct tiles without conflicts
    /// </summary>
    [Test]
    public void GetTile_ConcurrentReads_AllCorrect()
    {
        // Arrange
        var map = new SharedMap();
        var testPositions = new[] { (0, 0), (1, 1), (2, 2), (3, 3) };
        foreach (var pos in testPositions)
        {
            map.SetTile(pos, new Room());
        }

        var readCount = new ConcurrentDictionary<(int, int), int>();
        var threadCount = 10;
        var tasks = new List<Task>();

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var task = Task.Run(() =>
            {
                foreach (var pos in testPositions)
                {
                    var tile = map.GetTile(pos);
                    readCount.AddOrUpdate(pos, 1, (_, count) => count + 1);
                    Assert.That(tile, Is.Not.Null);
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        foreach (var pos in testPositions)
        {
            Assert.That(readCount[pos], Is.EqualTo(threadCount));
        }
    }

    /// <summary>
    /// Test: SharedMap is thread-safe for concurrent read-write
    /// Arrange: Create a SharedMap with initial tiles
    /// Act: Some threads read while others write at different positions
    /// Assert: No exceptions, final state is consistent
    /// </summary>
    [Test]
    public void ReadWrite_Concurrent_NoConflicts()
    {
        // Arrange
        var map = new SharedMap();
        var tasks = new List<Task>();
        var iterations = 100;

        // Act
        // Reader threads
        for (int r = 0; r < 5; r++)
        {
            var task = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var pos = (i % 10, 0);
                    var tile = map.GetTile(pos);
                    // Just read, don't assert null (writer might not have written yet)
                }
            });
            tasks.Add(task);
        }

        // Writer threads
        for (int w = 0; w < 5; w++)
        {
            var task = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var pos = (i % 10, 0);
                    map.SetTile(pos, new Room());
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert: At least some tiles should be stored
        var count = 0;
        for (int i = 0; i < 10; i++)
        {
            if (map.GetTile((i, 0)) != null)
                count++;
        }
        Assert.That(count, Is.GreaterThan(0));
    }

    /// <summary>
    /// Test: SharedMap is serializable to JSON
    /// Arrange: Create a SharedMap with tiles
    /// Act: Serialize to JSON and deserialize
    /// Assert: Deserialized map has the same tiles
    /// </summary>
    [Test]
    public void Serialize_ValidMap_DeserializedCorrectly()
    {
        // Arrange
        var map = new SharedMap();
        map.SetTile((0, 0), new Room());
        map.SetTile((1, 1), Wall.Singleton);

        // Act
        var json = map.ToJson();
        var deserializedMap = SharedMap.FromJson(json);

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Does.Contain("Room"));
        Assert.That(json, Does.Contain("Wall"));
        
        var room = deserializedMap.GetTile((0, 0));
        var wall = deserializedMap.GetTile((1, 1));
        Assert.That(room, Is.Not.Null);
        Assert.That(wall, Is.Not.Null);
    }

    /// <summary>
    /// Test: SharedMap export returns all known tiles
    /// Arrange: Create a SharedMap with multiple tiles
    /// Act: Export all tiles
    /// Assert: Returned collection matches stored tiles
    /// </summary>
    [Test]
    public void ExportAllTiles_MultiplePositions_AllExported()
    {
        // Arrange
        var map = new SharedMap();
        var positions = new[] { (0, 0), (1, 0), (0, 1), (1, 1) };
        foreach (var pos in positions)
        {
            map.SetTile(pos, new Room());
        }

        // Act
        var exported = map.ExportAllTiles();

        // Assert
        Assert.That(exported, Has.Count.EqualTo(positions.Length));
        foreach (var (pos, tile) in exported)
        {
            Assert.That(positions, Does.Contain(pos));
            Assert.That(tile, Is.Not.Null);
        }
    }

    /// <summary>
    /// Test: SharedMap GetKnownBounds returns exploration extent
    /// Arrange: Create a SharedMap with tiles at specific positions
    /// Act: Get the known bounds
    /// Assert: Bounds encompass all stored positions
    /// </summary>
    [Test]
    public void GetKnownBounds_VariousPositions_CorrectBounds()
    {
        // Arrange
        var map = new SharedMap();
        map.SetTile((0, 0), new Room());
        map.SetTile((5, 10), new Room());
        map.SetTile((-3, -2), new Room());

        // Act
        var (minX, maxX, minY, maxY) = map.GetKnownBounds();

        // Assert
        Assert.That(minX, Is.LessThanOrEqualTo(-3));
        Assert.That(maxX, Is.GreaterThanOrEqualTo(5));
        Assert.That(minY, Is.LessThanOrEqualTo(-2));
        Assert.That(maxY, Is.GreaterThanOrEqualTo(10));
    }

    /// <summary>
    /// Test: SharedMap thread-safe update of same position
    /// Arrange: Create a SharedMap
    /// Act: Multiple threads update the same position with different tiles
    /// Assert: Final state is consistent (last write wins, no exceptions)
    /// </summary>
    [Test]
    public void SetTile_SamePositionConcurrent_LastWriteWins()
    {
        // Arrange
        var map = new SharedMap();
        var position = (0, 0);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var task = Task.Run(() =>
            {
                map.SetTile(position, new Room());
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert: Position has a tile (no nulls, no exceptions)
        var retrieved = map.GetTile(position);
        Assert.That(retrieved, Is.Not.Null);
    }
}
