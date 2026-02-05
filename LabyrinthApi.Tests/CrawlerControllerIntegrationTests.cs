using System.Net;
using System.Net.Http.Json;
using ApiTypes;
using LabyrinthApi.Controllers.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LabyrinthApi.Tests;

/// <summary>
/// Integration tests for the Crawler API endpoints following AAA pattern.
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
    public async Task PostCrawler_WithValidRequest_ShouldReturnCreatedCrawler()
    {
        // Arrange
        var request = new CreateCrawlerRequest { AppKey = ValidAppKey };

        // Act
        var response = await _client.PostAsJsonAsync("/crawlers", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var crawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(crawler, Is.Not.Null);
        Assert.That(crawler!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task PostCrawler_WithMissingAppKey_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateCrawlerRequest { AppKey = null };

        // Act
        var response = await _client.PostAsJsonAsync("/crawlers", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostCrawler_WithEmptyAppKey_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateCrawlerRequest { AppKey = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/crawlers", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    #endregion

    #region GET /crawlers/{id} Tests

    [Test]
    public async Task GetCrawler_WithExistingId_ShouldReturnCrawler()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        // Act
        var response = await _client.GetAsync($"/crawlers/{createdCrawler!.Id}");

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

        // Act
        var response = await _client.GetAsync($"/crawlers/{nonExistingId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    #endregion

    #region PATCH /crawlers/{id} Tests

    [Test]
    public async Task PatchCrawler_WithValidPositionUpdate_ShouldReturnUpdatedCrawler()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var updateRequest = new UpdateCrawlerRequest
        {
            AppKey = ValidAppKey,
            X = 10,
            Y = 20,
            Direction = Direction.South
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/crawlers/{createdCrawler!.Id}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCrawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updatedCrawler, Is.Not.Null);
        Assert.That(updatedCrawler!.X, Is.EqualTo(10));
        Assert.That(updatedCrawler.Y, Is.EqualTo(20));
        Assert.That(updatedCrawler.Dir, Is.EqualTo(Direction.South));
    }

    [Test]
    public async Task PatchCrawler_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateCrawlerRequest
        {
            AppKey = ValidAppKey,
            X = 5,
            Y = 5
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/crawlers/{nonExistingId}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PatchCrawler_WithMissingAppKey_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var updateRequest = new UpdateCrawlerRequest { AppKey = null, X = 5 };

        // Act
        var response = await _client.PatchAsJsonAsync($"/crawlers/{createdCrawler!.Id}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PatchCrawler_WithPartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();
        var originalDirection = createdCrawler!.Dir;

        var updateRequest = new UpdateCrawlerRequest
        {
            AppKey = ValidAppKey,
            X = 15  // Only update X
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/crawlers/{createdCrawler.Id}", updateRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedCrawler = await response.Content.ReadFromJsonAsync<Crawler>();
        Assert.That(updatedCrawler!.X, Is.EqualTo(15));
        Assert.That(updatedCrawler.Y, Is.EqualTo(createdCrawler.Y));
        Assert.That(updatedCrawler.Dir, Is.EqualTo(originalDirection));
    }

    #endregion

    #region DELETE /crawlers/{id} Tests

    [Test]
    public async Task DeleteCrawler_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var deleteRequest = new DeleteCrawlerRequest { AppKey = ValidAppKey };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/crawlers/{createdCrawler!.Id}")
        {
            Content = JsonContent.Create(deleteRequest)
        };
        var response = await _client.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DeleteCrawler_WithExistingId_CrawlerShouldNotExistAfterwards()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var deleteRequest = new DeleteCrawlerRequest { AppKey = ValidAppKey };
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/crawlers/{createdCrawler!.Id}")
        {
            Content = JsonContent.Create(deleteRequest)
        };
        await _client.SendAsync(request);

        // Act
        var getResponse = await _client.GetAsync($"/crawlers/{createdCrawler.Id}");

        // Assert
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteCrawler_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var deleteRequest = new DeleteCrawlerRequest { AppKey = ValidAppKey };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/crawlers/{nonExistingId}")
        {
            Content = JsonContent.Create(deleteRequest)
        };
        var response = await _client.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteCrawler_WithMissingAppKey_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateCrawlerRequest { AppKey = ValidAppKey };
        var createResponse = await _client.PostAsJsonAsync("/crawlers", createRequest);
        var createdCrawler = await createResponse.Content.ReadFromJsonAsync<Crawler>();

        var deleteRequest = new DeleteCrawlerRequest { AppKey = null };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/crawlers/{createdCrawler!.Id}")
        {
            Content = JsonContent.Create(deleteRequest)
        };
        var response = await _client.SendAsync(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    #endregion
}

