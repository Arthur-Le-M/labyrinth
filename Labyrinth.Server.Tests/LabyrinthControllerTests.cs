using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ApiTypes;

namespace Labyrinth.Server.Tests;

/// <summary>
/// Integration tests for the LabyrinthController following TDD and AAA pattern.
/// </summary>
[TestFixture]
public class LabyrinthControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region GET /labyrinths/{labyId}/tiles Tests

    [Test]
    public async Task GetTiles_WithValidLabyrinthId_ReturnsOkWithTiles()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tiles = await response.Content.ReadFromJsonAsync<TileDto[]>();
        Assert.That(tiles, Is.Not.Null);
        Assert.That(tiles!.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetTiles_WithInvalidLabyrinthId_ReturnsNotFound()
    {
        // Arrange
        const string labyId = "invalid";

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTiles_With9x7Labyrinth_ReturnsCorrectNumberOfTiles()
    {
        // Arrange
        const string labyId = "9x7";
        const int expectedWidth = 18;
        const int expectedHeight = 7;
        const int expectedTileCount = expectedWidth * expectedHeight;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tiles = await response.Content.ReadFromJsonAsync<TileDto[]>();
        Assert.That(tiles!.Length, Is.EqualTo(expectedTileCount));
    }

    [Test]
    public async Task GetTiles_With17x19Labyrinth_ReturnsCorrectNumberOfTiles()
    {
        // Arrange
        const string labyId = "17x19";
        const int expectedWidth = 17;
        const int expectedHeight = 15; // Based on the JSON preview
        const int expectedTileCount = expectedWidth * expectedHeight;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tiles = await response.Content.ReadFromJsonAsync<TileDto[]>();
        Assert.That(tiles!.Length, Is.EqualTo(expectedTileCount));
    }

    [Test]
    public async Task GetTiles_ReturnsCorrectTileTypes()
    {
        // Arrange
        const string labyId = "9x7";

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles");

        // Assert
        var tiles = await response.Content.ReadFromJsonAsync<TileDto[]>();
        Assert.That(tiles, Is.Not.Null);
        
        // Should contain walls (border of labyrinth)
        Assert.That(tiles!.Any(t => t.Type == TileType.Wall), Is.True);
        // Should contain rooms (passages)
        Assert.That(tiles!.Any(t => t.Type == TileType.Room), Is.True);
        // Should contain doors
        Assert.That(tiles!.Any(t => t.Type == TileType.Door), Is.True);
    }

    #endregion

    #region GET /labyrinths/{labyId}/tiles/{x}/{y} Tests

    [Test]
    public async Task GetTile_WithValidCoordinates_ReturnsOkWithTile()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 0;
        const int y = 0;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tile = await response.Content.ReadFromJsonAsync<TileDto>();
        Assert.That(tile, Is.Not.Null);
        Assert.That(tile!.X, Is.EqualTo(x));
        Assert.That(tile.Y, Is.EqualTo(y));
    }

    [Test]
    public async Task GetTile_WithInvalidLabyrinthId_ReturnsNotFound()
    {
        // Arrange
        const string labyId = "invalid";
        const int x = 0;
        const int y = 0;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTile_WithOutOfBoundsCoordinates_ReturnsNotFound()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 100;
        const int y = 100;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTile_WithNegativeCoordinates_ReturnsNotFound()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = -1;
        const int y = 0;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTile_CornerTile_ReturnsRoom()
    {
        // Arrange - based on the 9x7 labyrinth preview: " # # # d # # # # #", (0,0) is a space
        const string labyId = "9x7";
        const int x = 0;
        const int y = 0;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        var tile = await response.Content.ReadFromJsonAsync<TileDto>();
        Assert.That(tile!.Type, Is.EqualTo(TileType.Room));
    }

    [Test]
    public async Task GetTile_DoorPosition_ReturnsDoor()
    {
        // Arrange - based on the 9x7 labyrinth preview: " # # # d # # # # #", (7, 0) is 'd'
        const string labyId = "9x7";
        const int x = 7;
        const int y = 0;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        var tile = await response.Content.ReadFromJsonAsync<TileDto>();
        Assert.That(tile!.Type, Is.EqualTo(TileType.Door));
    }

    [Test]
    public async Task GetTile_ReturnsTileWithCorrectCoordinates()
    {
        // Arrange
        const string labyId = "9x7";
        const int x = 4;
        const int y = 3;

        // Act
        var response = await _client.GetAsync($"/labyrinths/{labyId}/tiles/{x}/{y}");

        // Assert
        var tile = await response.Content.ReadFromJsonAsync<TileDto>();
        Assert.That(tile!.X, Is.EqualTo(x));
        Assert.That(tile.Y, Is.EqualTo(y));
    }

    #endregion
}

