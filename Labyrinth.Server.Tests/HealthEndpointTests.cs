using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Labyrinth.Server.Tests;

/// <summary>
/// Integration tests for the Health endpoint following AAA pattern (Arrange-Act-Assert).
/// </summary>
[TestFixture]
public class HealthEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

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

    [Test]
    public async Task GetHealth_ReturnsOk()
    {
        // Arrange
        var requestUri = "/health";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

