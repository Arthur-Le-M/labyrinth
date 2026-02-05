using LabyrinthApi.Services;
using ApiTypes;

namespace LabyrinthApi.Tests;

/// <summary>
/// Unit tests for LabyrinthService following TDD and AAA pattern.
/// </summary>
[TestFixture]
public class LabyrinthServiceTests
{
    private ILabyrinthService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new LabyrinthService();
    }

    #region LabyrinthExists Tests

    [Test]
    public void LabyrinthExists_WithValidId9x7_ReturnsTrue()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var result = _service.LabyrinthExists(labyId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void LabyrinthExists_WithValidId17x19_ReturnsTrue()
    {
        // Arrange
        const string labyId = "17x19";

        // Act
        var result = _service.LabyrinthExists(labyId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void LabyrinthExists_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        const string labyId = "invalid";

        // Act
        var result = _service.LabyrinthExists(labyId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LabyrinthExists_WithEmptyId_ReturnsFalse()
    {
        // Arrange
        const string labyId = "";

        // Act
        var result = _service.LabyrinthExists(labyId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetAllTiles Tests

    [Test]
    public void GetAllTiles_WithValidLabyrinth_ReturnsNonEmptyList()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var result = _service.GetAllTiles(labyId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.GreaterThan(0));
    }

    [Test]
    public void GetAllTiles_With9x7Labyrinth_ReturnsCorrectCount()
    {
        // Arrange
        const string labyId = "9x7";
        // Preview line: " # # # d # # # # #" = 18 chars wide, 7 lines
        const int expectedCount = 18 * 7;

        // Act
        var result = _service.GetAllTiles(labyId);

        // Assert
        Assert.That(result!.Count, Is.EqualTo(expectedCount));
    }

    [Test]
    public void GetAllTiles_WithInvalidLabyrinth_ReturnsNull()
    {
        // Arrange
        const string labyId = "invalid";

        // Act
        var result = _service.GetAllTiles(labyId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAllTiles_ContainsAllTileTypes()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var result = _service.GetAllTiles(labyId);

        // Assert
        Assert.That(result!.Any(t => t.Type == TileType.Wall), Is.True);
        Assert.That(result.Any(t => t.Type == TileType.Room), Is.True);
        Assert.That(result.Any(t => t.Type == TileType.Door), Is.True);
    }

    [Test]
    public void GetAllTiles_TilesHaveCorrectCoordinates()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var result = _service.GetAllTiles(labyId);

        // Assert
        // Preview line: " # # # d # # # # #" = 18 chars wide
        Assert.That(result!.All(t => t.X >= 0 && t.X < 18), Is.True);
        // All Y coordinates should be in range [0, 6]
        Assert.That(result.All(t => t.Y >= 0 && t.Y < 7), Is.True);
    }

    #endregion

    #region GetTile Tests

    [Test]
    public void GetTile_WithValidCoordinates_ReturnsTile()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 0;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.X, Is.EqualTo(x));
        Assert.That(result.Y, Is.EqualTo(y));
    }

    [Test]
    public void GetTile_WithInvalidLabyrinth_ReturnsNull()
    {
        // Arrange
        const string labyId = "invalid";
        const int x = 0;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTile_WithOutOfBoundsX_ReturnsNull()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 100;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTile_WithOutOfBoundsY_ReturnsNull()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 0;
        const int y = 100;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTile_WithNegativeX_ReturnsNull()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = -1;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTile_WithNegativeY_ReturnsNull()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 0;
        const int y = -1;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTile_CornerPosition_ReturnsRoom()
    {
        // Arrange - based on " # # # d # # # # #", position (0,0) is a space (Room)
        const string labyId = "9x7";
        const int x = 0;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result!.Type, Is.EqualTo(TileType.Room));
    }

    [Test]
    public void GetTile_DoorPosition_ReturnsDoor()
    {
        // Arrange - based on " # # # d # # # # #", 'd' is at position (7, 0)
        const string labyId = "9x7";
        const int x = 7;
        const int y = 0;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result!.Type, Is.EqualTo(TileType.Door));
    }

    [Test]
    public void GetTile_RoomPosition_ReturnsRoom()
    {
        // Arrange - based on " # k             #", position (3, 1) is 'k' (Room with key)
        const string labyId = "9x7";
        const int x = 3;
        const int y = 1;

        // Act
        var result = _service.GetTile(labyId, x, y);

        // Assert
        Assert.That(result!.Type, Is.EqualTo(TileType.Room));
    }

    #endregion
}

