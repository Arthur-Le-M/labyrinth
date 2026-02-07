using System.Net;
using System.Net.Http.Json;
using ApiTypes;
using Labyrinth.Server.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Labyrinth.Server.Tests;

/// <summary>
/// Integration tests for the Crawler API endpoints following AAA pattern.
/// Updated to match the official Labyrinth API specification.
/// </summary>
[TestFixture]
public class CrawlerControllerIntegrationTests
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

    #region POST /crawlers Tests

    [Test]
    public async Task PostCrawler_WithValidAppKeyInQuery_ShouldReturnCreatedCrawler()
    {
        // Arrange - appKey in query parameter only (official API)

        // Act
        var response = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var crawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(crawler, Is.Not.Null);
        Assert.That(crawler!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task PostCrawler_WithSettings_ShouldReturnCreatedCrawler()
    {
        // Arrange - appKey in query, optional Settings in body
        var settings = new Settings { RandomSeed = 42 };

        // Act
        var response = await _client.PostAsJsonAsync($"/crawlers?appKey={ValidAppKey}", settings);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var crawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(crawler, Is.Not.Null);
    }

    [Test]
    public async Task PostCrawler_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange & Act - No appKey
        var response = await _client.PostAsync("/crawlers", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostCrawler_WithEmptyAppKey_ShouldReturnUnauthorized()
    {
        // Arrange & Act
        var response = await _client.PostAsync("/crawlers?appKey=", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostCrawler_ExceedingLimit_ShouldReturnForbidden()
    {
        // Arrange - create 3 crawlers
        var uniqueAppKey = Guid.NewGuid().ToString();
        await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);
        await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);
        await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);

        // Act - try to create 4th
        var response = await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    #endregion

    #region GET /crawlers/{id} Tests

    [Test]
    public async Task GetCrawler_WithExistingId_ShouldReturnCrawler()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - appKey in query parameter
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var crawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(crawler, Is.Not.Null);
        Assert.That(crawler!.Id, Is.EqualTo(createdCrawler.Id));
    }

    [Test]
    public async Task GetCrawler_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act - appKey in query parameter
        var response = await _client.GetAsync($"/crawlers/{nonExistingId}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetCrawler_WithoutAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - No appKey
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCrawler_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - Different appKey
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}?appKey=wrong-key");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    #endregion

    #region PATCH /crawlers/{id} Tests

    [Test]
    public async Task PatchCrawler_WithDirectionChange_ShouldReturnUpdatedCrawler()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var updateRequest = new CrawlerUpdateRequest { Direction = Direction.South };

        // Act - appKey in query parameter
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCrawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updatedCrawler, Is.Not.Null);
        Assert.That(updatedCrawler!.Dir, Is.EqualTo(Direction.South));
    }

    [Test]
    public async Task PatchCrawler_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new CrawlerUpdateRequest { Direction = Direction.East };

        // Act - appKey in query parameter
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{nonExistingId}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PatchCrawler_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var updateRequest = new CrawlerUpdateRequest { Direction = Direction.West };

        // Act - No appKey
        var response = await _client.PatchAsJsonAsync($"/crawlers/{createdCrawler!.Id}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PatchCrawler_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var updateRequest = new CrawlerUpdateRequest { Direction = Direction.West };

        // Act - Wrong appKey
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}?appKey=wrong-key", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PatchCrawler_WithDirectionOnlyUpdate_ShouldNotChangePosition()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();
        var originalX = createdCrawler!.X;
        var originalY = createdCrawler.Y;

        var updateRequest = new CrawlerUpdateRequest { Direction = Direction.West };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/crawlers/{createdCrawler.Id}?appKey={ValidAppKey}", 
            updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCrawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updatedCrawler!.X, Is.EqualTo(originalX));
        Assert.That(updatedCrawler.Y, Is.EqualTo(originalY));
        Assert.That(updatedCrawler.Dir, Is.EqualTo(Direction.West));
    }

    #endregion

    #region DELETE /crawlers/{id} Tests

    [Test]
    public async Task DeleteCrawler_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - appKey in query parameter
        var response = await _client.DeleteAsync($"/crawlers/{createdCrawler!.Id}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DeleteCrawler_WithExistingId_CrawlerShouldNotExistAfterwards()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        await _client.DeleteAsync($"/crawlers/{createdCrawler!.Id}?appKey={ValidAppKey}");

        // Act
        var getResponse = await _client.GetAsync($"/crawlers/{createdCrawler.Id}?appKey={ValidAppKey}");

        // Assert
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteCrawler_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act - appKey in query parameter
        var response = await _client.DeleteAsync($"/crawlers/{nonExistingId}?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteCrawler_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - No appKey
        var response = await _client.DeleteAsync($"/crawlers/{createdCrawler!.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeleteCrawler_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act - Wrong appKey
        var response = await _client.DeleteAsync($"/crawlers/{createdCrawler!.Id}?appKey=wrong-key");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    #endregion

    #region GET /crawlers Tests

    [Test]
    public async Task GetCrawlers_WithValidAppKey_ShouldReturnCrawlersForApp()
    {
        // Arrange
        var uniqueAppKey = Guid.NewGuid().ToString();
        await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);
        await _client.PostAsync($"/crawlers?appKey={uniqueAppKey}", null);

        // Act
        var response = await _client.GetAsync($"/crawlers?appKey={uniqueAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var crawlers = await response.Content.ReadFromJsonAsync<Crawler[]>();
        Assert.That(crawlers, Is.Not.Null);
        Assert.That(crawlers!.Length, Is.EqualTo(2));
    }

    [Test]
    public async Task GetCrawlers_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/crawlers");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCrawlers_WithEmptyAppKey_ShouldReturnUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/crawlers?appKey=");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCrawlers_WithNoMatchingCrawlers_ShouldReturnEmptyArray()
    {
        // Arrange
        var uniqueAppKey = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/crawlers?appKey={uniqueAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var crawlers = await response.Content.ReadFromJsonAsync<Crawler[]>();
        Assert.That(crawlers, Is.Not.Null);
        Assert.That(crawlers, Is.Empty);
    }

    #endregion

    #region GET /crawlers/{id}/bag Tests

    [Test]
    public async Task GetBag_WithValidAppKey_ShouldReturnBag()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/bag?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bag = await response.Content.ReadFromJsonAsync<InventoryItem[]>();
        Assert.That(bag, Is.Not.Null);
    }

    [Test]
    public async Task GetBag_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/bag");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetBag_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/bag?appKey=wrong-key");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetBag_WithNonExistingCrawler_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/crawlers/{nonExistingId}/bag?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region GET /crawlers/{id}/items Tests

    [Test]
    public async Task GetItems_WithValidAppKey_ShouldReturnItems()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/items?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var items = await response.Content.ReadFromJsonAsync<InventoryItem[]>();
        Assert.That(items, Is.Not.Null);
    }

    [Test]
    public async Task GetItems_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/items");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetItems_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}/items?appKey=wrong-key");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetItems_WithNonExistingCrawler_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/crawlers/{nonExistingId}/items?appKey={ValidAppKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region PUT /crawlers/{id}/bag Tests

    [Test]
    public async Task PutBag_WithValidAppKey_ShouldReturnUpdatedBag()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[]
        {
            new InventoryItem { Type = ItemType.Key, MoveRequired = true }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}/bag?appKey={ValidAppKey}", 
            moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PutBag_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key } };

        // Act
        var response = await _client.PutAsJsonAsync($"/crawlers/{createdCrawler!.Id}/bag", moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PutBag_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key } };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}/bag?appKey=wrong-key", 
            moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    #endregion

    #region PUT /crawlers/{id}/items Tests

    [Test]
    public async Task PutItems_WithValidAppKey_ShouldReturnUpdatedItems()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[]
        {
            new InventoryItem { Type = ItemType.Key, MoveRequired = true }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}/items?appKey={ValidAppKey}", 
            moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PutItems_WithMissingAppKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key } };

        // Act
        var response = await _client.PutAsJsonAsync($"/crawlers/{createdCrawler!.Id}/items", moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PutItems_WithWrongAppKey_ShouldReturnForbidden()
    {
        // Arrange
        var createResponse = await _client.PostAsync($"/crawlers?appKey={ValidAppKey}", null);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key } };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/crawlers/{createdCrawler!.Id}/items?appKey=wrong-key", 
            moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PutItems_WithNonExistingCrawler_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var moveRequests = new[]
        {
            new InventoryItem { Type = ItemType.Key, MoveRequired = true }
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/crawlers/{nonExistingId}/items?appKey={ValidAppKey}", 
            moveRequests);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion
}

