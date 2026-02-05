using ApiTypes;
using LabyrinthApi.Services;

namespace LabyrinthApi.Tests;

/// <summary>
/// Unit tests for CrawlerService following AAA pattern (Arrange-Act-Assert).
/// </summary>
[TestFixture]
public class CrawlerServiceTests
{
    private CrawlerService _crawlerService = null!;

    [SetUp]
    public void SetUp()
    {
        _crawlerService = new CrawlerService();
    }

    #region AddCrawler Tests

    [Test]
    public void AddCrawler_WithValidCrawler_ShouldStoreCrawler()
    {
        // Arrange
        var crawler = new Crawler { Id = Guid.NewGuid(), X = 0, Y = 0, Dir = Direction.North };

        // Act
        _crawlerService.AddCrawler(crawler);

        // Assert
        Assert.That(_crawlerService.CrawlerExists(crawler.Id), Is.True);
    }

    [Test]
    public void AddCrawler_WithDuplicateId_ShouldOverwriteExistingCrawler()
    {
        // Arrange
        var id = Guid.NewGuid();
        var originalCrawler = new Crawler { Id = id, X = 0, Y = 0, Dir = Direction.North };
        var newCrawler = new Crawler { Id = id, X = 5, Y = 10, Dir = Direction.South };

        // Act
        _crawlerService.AddCrawler(originalCrawler);
        _crawlerService.AddCrawler(newCrawler);
        var result = _crawlerService.GetCrawler(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.X, Is.EqualTo(5));
        Assert.That(result.Y, Is.EqualTo(10));
        Assert.That(result.Dir, Is.EqualTo(Direction.South));
    }

    #endregion

    #region GetCrawler Tests

    [Test]
    public void GetCrawler_WithExistingId_ShouldReturnCrawler()
    {
        // Arrange
        var crawler = new Crawler { Id = Guid.NewGuid(), X = 3, Y = 4, Dir = Direction.East };
        _crawlerService.AddCrawler(crawler);

        // Act
        var result = _crawlerService.GetCrawler(crawler.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(crawler.Id));
        Assert.That(result.X, Is.EqualTo(3));
        Assert.That(result.Y, Is.EqualTo(4));
        Assert.That(result.Dir, Is.EqualTo(Direction.East));
    }

    [Test]
    public void GetCrawler_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = _crawlerService.GetCrawler(nonExistingId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region CrawlerExists Tests

    [Test]
    public void CrawlerExists_WithExistingCrawler_ShouldReturnTrue()
    {
        // Arrange
        var crawler = new Crawler { Id = Guid.NewGuid(), X = 0, Y = 0, Dir = Direction.North };
        _crawlerService.AddCrawler(crawler);

        // Act
        var result = _crawlerService.CrawlerExists(crawler.Id);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CrawlerExists_WithNonExistingCrawler_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = _crawlerService.CrawlerExists(nonExistingId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region UpdateCrawler Tests

    [Test]
    public void UpdateCrawler_WithExistingCrawler_ShouldUpdateState()
    {
        // Arrange
        var id = Guid.NewGuid();
        var originalCrawler = new Crawler { Id = id, X = 0, Y = 0, Dir = Direction.North };
        _crawlerService.AddCrawler(originalCrawler);
        
        var updatedCrawler = new Crawler { Id = id, X = 10, Y = 20, Dir = Direction.West, Walking = true };

        // Act
        _crawlerService.UpdateCrawler(updatedCrawler);
        var result = _crawlerService.GetCrawler(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.X, Is.EqualTo(10));
        Assert.That(result.Y, Is.EqualTo(20));
        Assert.That(result.Dir, Is.EqualTo(Direction.West));
        Assert.That(result.Walking, Is.True);
    }

    [Test]
    public void UpdateCrawler_WithNonExistingCrawler_ShouldNotAddCrawler()
    {
        // Arrange
        var nonExistingCrawler = new Crawler { Id = Guid.NewGuid(), X = 5, Y = 5, Dir = Direction.East };

        // Act
        _crawlerService.UpdateCrawler(nonExistingCrawler);

        // Assert
        Assert.That(_crawlerService.CrawlerExists(nonExistingCrawler.Id), Is.False);
    }

    #endregion

    #region DeleteCrawler Tests

    [Test]
    public void DeleteCrawler_WithExistingCrawler_ShouldRemoveCrawler()
    {
        // Arrange
        var crawler = new Crawler { Id = Guid.NewGuid(), X = 0, Y = 0, Dir = Direction.North };
        _crawlerService.AddCrawler(crawler);

        // Act
        var result = _crawlerService.DeleteCrawler(crawler.Id);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_crawlerService.CrawlerExists(crawler.Id), Is.False);
    }

    [Test]
    public void DeleteCrawler_WithNonExistingCrawler_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = _crawlerService.DeleteCrawler(nonExistingId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion
}

