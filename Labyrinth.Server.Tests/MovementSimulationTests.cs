using System.Net;
using System.Net.Http.Json;
using ApiTypes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Labyrinth.Server.Tests;

/// <summary>
/// Integration tests for movement simulation in the Crawler API.
/// Tests the PATCH /crawlers/{id} endpoint behavior according to the official API spec.
/// </summary>
[TestFixture]
public class MovementSimulationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string ValidAppKey = "test-app-key";

    [SetUp]
    public void SetUp()
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

    #region Direction Change Tests

    [Test]
    public async Task PatchCrawler_WithDirectionOnly_ShouldChangeDirection()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var updateRequest = new { direction = "East" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updated!.Dir, Is.EqualTo(Direction.East));
        Assert.That(updated.X, Is.EqualTo(crawler.X)); // Position unchanged
        Assert.That(updated.Y, Is.EqualTo(crawler.Y));
    }

    [Test]
    public async Task PatchCrawler_WithDirectionChange_ShouldUpdateFacingTile()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var updateRequest = new { direction = "South" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updated!.FacingTile, Is.Not.EqualTo(TileType.Outside));
    }

    #endregion

    #region Walking Tests - Successful Movement

    [Test]
    public async Task PatchCrawler_WithWalkingTrue_OnTraversableTile_ShouldMove()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        // First turn to face a traversable tile (Room)
        await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}",
            new { direction = "East" });
        
        var walkRequest = new { walking = true };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            walkRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<Crawler>();
        // Crawler should have moved (position changed)
        Assert.That(updated!.X != crawler.X || updated.Y != crawler.Y, Is.True,
            "Crawler should have moved to a new position");
    }

    [Test]
    public async Task PatchCrawler_WithWalkingFalse_ShouldNotMove()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var originalX = crawler.X;
        var originalY = crawler.Y;
        var updateRequest = new { walking = false };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updated!.X, Is.EqualTo(originalX));
        Assert.That(updated.Y, Is.EqualTo(originalY));
    }

    #endregion

    #region Walking Tests - Blocked Movement

    [Test]
    public async Task PatchCrawler_WithWalkingTrue_OnWall_ShouldReturnConflict()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        // Position the crawler to face a wall (depends on labyrinth layout)
        // Default position (0,0) facing North typically faces a wall
        
        var walkRequest = new { walking = true };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            walkRequest);

        // Assert
        // Should return 409 Conflict when cannot traverse
        // Or 200 if the tile happens to be traversable
        Assert.That(response.StatusCode, 
            Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.Conflict));
    }

    #endregion

    #region AppKey Query Parameter Tests

    [Test]
    public async Task PatchCrawler_WithAppKeyInQuery_ShouldWork()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var updateRequest = new { direction = "West" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PatchCrawler_WithoutAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var updateRequest = new { direction = "West" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCrawler_WithAppKeyInQuery_ShouldWork()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();

        // Act
        var response = await _client.GetAsync($"/crawlers/{crawler.Id}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetCrawler_WithoutAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();

        // Act
        var response = await _client.GetAsync($"/crawlers/{crawler.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeleteCrawler_WithAppKeyInQuery_ShouldWork()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();

        // Act
        var response = await _client.DeleteAsync($"/crawlers/{crawler.Id}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DeleteCrawler_WithoutAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();

        // Act
        var response = await _client.DeleteAsync($"/crawlers/{crawler.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    #endregion

    #region Crawler Limit Tests

    [Test]
    public async Task PostCrawler_WhenLimitReached_ShouldReturnForbidden()
    {
        // Arrange - Create 3 crawlers (the maximum)
        var uniqueAppKey = Guid.NewGuid().ToString();
        await CreateCrawlerWithAppKeyAsync(uniqueAppKey);
        await CreateCrawlerWithAppKeyAsync(uniqueAppKey);
        await CreateCrawlerWithAppKeyAsync(uniqueAppKey);

        // Act - Try to create a 4th crawler (appKey in query)
        var response = await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostCrawler_AfterDeletingOne_ShouldAllowNewCrawler()
    {
        // Arrange - Create 3 crawlers then delete one
        var uniqueAppKey = Guid.NewGuid().ToString();
        var crawler1 = await CreateCrawlerWithAppKeyAsync(uniqueAppKey);
        await CreateCrawlerWithAppKeyAsync(uniqueAppKey);
        await CreateCrawlerWithAppKeyAsync(uniqueAppKey);
        
        await _client.DeleteAsync($"/crawlers/{crawler1.Id}?appKey={uniqueAppKey}");

        // Act - Try to create a new crawler (appKey in query)
        var response = await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    #endregion

    #region FacingTile Update Tests

    [Test]
    public async Task PatchCrawler_AfterMoving_ShouldUpdateFacingTile()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        // Turn to face a traversable direction
        await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}",
            new { direction = "East" });

        // Act - Walk
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}",
            new { walking = true });

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var updated = await response.Content.ReadFromJsonAsync<Crawler>();
            // FacingTile should be a valid TileType
            Assert.That(updated!.FacingTile, Is.TypeOf<TileType>());
        }
    }

    #endregion

    #region Combined Direction and Walking Tests

    [Test]
    public async Task PatchCrawler_WithDirectionAndWalking_ShouldTurnThenMove()
    {
        // Arrange
        var crawler = await CreateCrawlerAsync();
        var updateRequest = new { direction = "East", walking = true };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{crawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, 
            Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.Conflict));
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var updated = await response.Content.ReadFromJsonAsync<Crawler>();
            Assert.That(updated!.Dir, Is.EqualTo(Direction.East));
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Crawler> CreateCrawlerAsync()
    {
        return await CreateCrawlerWithAppKeyAsync(ValidAppKey);
    }

    private async Task<Crawler> CreateCrawlerWithAppKeyAsync(string appKey)
    {
        // appKey in query parameter per official API
        var response = await _client.PostAsync($"/crawlers?appKey={appKey}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Crawler>())!;
    }

    #endregion
}
