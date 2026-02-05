namespace Labyrinth.Server.Tests;

using NUnit.Framework;
using Moq;
using ApiTypes;
using Labyrinth.Server.Services;

/// <summary>
/// Unit tests for InventoryService following the AAA pattern (Arrange, Act, Assert).
/// </summary>
[TestFixture]
public class InventoryServiceTests
{
    private Mock<ICrawlerService> _mockCrawlerService = null!;
    private InventoryService _inventoryService = null!;
    
    [SetUp]
    public void Setup()
    {
        _mockCrawlerService = new Mock<ICrawlerService>();
        _inventoryService = new InventoryService(_mockCrawlerService.Object);
    }
    
    #region GetBag Tests
    
    [Test]
    public void GetBag_WhenCrawlerExists_ReturnsBagItems()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var expectedBag = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler { Id = crawlerId, Bag = expectedBag };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.GetBag(crawlerId);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result![0].Type, Is.EqualTo(ItemType.Key));
    }
    
    [Test]
    public void GetBag_WhenCrawlerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns((Crawler?)null);
        
        // Act
        var result = _inventoryService.GetBag(crawlerId);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void GetBag_WhenCrawlerExistsWithEmptyBag_ReturnsEmptyArray()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var crawler = new Crawler { Id = crawlerId, Bag = Array.Empty<InventoryItem>() };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.GetBag(crawlerId);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void GetBag_WhenCrawlerExistsWithNullBag_ReturnsNull()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var crawler = new Crawler { Id = crawlerId, Bag = null };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.GetBag(crawlerId);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    #endregion
    
    #region GetRoomItems Tests
    
    [Test]
    public void GetRoomItems_WhenCrawlerExists_ReturnsRoomItems()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var expectedItems = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler { Id = crawlerId, Items = expectedItems };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.GetRoomItems(crawlerId);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result![0].Type, Is.EqualTo(ItemType.Key));
    }
    
    [Test]
    public void GetRoomItems_WhenCrawlerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns((Crawler?)null);
        
        // Act
        var result = _inventoryService.GetRoomItems(crawlerId);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void GetRoomItems_WhenCrawlerExistsWithEmptyItems_ReturnsEmptyArray()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var crawler = new Crawler { Id = crawlerId, Items = Array.Empty<InventoryItem>() };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.GetRoomItems(crawlerId);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    
    #endregion
    
    #region MoveItems Tests
    
    [Test]
    public void MoveItems_WhenCrawlerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = true } };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns((Crawler?)null);
        
        // Act
        var result = _inventoryService.MoveItems(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void MoveItems_WhenNoMoveRequired_ReturnsSameBag()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var bag = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler { Id = crawlerId, Bag = bag, Items = Array.Empty<InventoryItem>() };
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = false } };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.MoveItems(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void MoveItems_WhenMoveRequired_RemovesItemFromBag()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var bag = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler { Id = crawlerId, Bag = bag, Items = Array.Empty<InventoryItem>() };
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = true } };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.MoveItems(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void MoveItems_WhenMoveRequired_AddsItemToRoomItems()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var bag = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler { Id = crawlerId, Bag = bag, Items = Array.Empty<InventoryItem>() };
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = true } };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        _inventoryService.MoveItems(crawlerId, moveRequests);
        
        // Assert
        Assert.That(crawler.Items, Is.Not.Null);
        Assert.That(crawler.Items, Has.Length.EqualTo(1));
        Assert.That(crawler.Items![0].Type, Is.EqualTo(ItemType.Key));
    }
    
    [Test]
    public void MoveItems_WithMultipleItems_MovesOnlyRequestedItems()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var bag = new[]
        {
            new InventoryItem { Type = ItemType.Key },
            new InventoryItem { Type = ItemType.Key },
            new InventoryItem { Type = ItemType.Key }
        };
        var crawler = new Crawler { Id = crawlerId, Bag = bag, Items = Array.Empty<InventoryItem>() };
        var moveRequests = new[]
        {
            new InventoryItem { Type = ItemType.Key, MoveRequired = true },
            new InventoryItem { Type = ItemType.Key, MoveRequired = false },
            new InventoryItem { Type = ItemType.Key, MoveRequired = true }
        };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        // Act
        var result = _inventoryService.MoveItems(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(1)); // One item should remain
        Assert.That(crawler.Items, Has.Length.EqualTo(2)); // Two items should be moved
    }
    
    #endregion
    
    #region MoveRoomItemsToBag Tests
    
    [Test]
    public void MoveRoomItemsToBag_WhenCrawlerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = true } };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns((Crawler?)null);
        
        // Act
        var result = _inventoryService.MoveRoomItemsToBag(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void MoveRoomItemsToBag_WhenMoveRequiredTrue_MovesItemFromRoomToBag()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var roomItems = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler 
        { 
            Id = crawlerId, 
            Bag = Array.Empty<InventoryItem>(),
            Items = roomItems 
        };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = true } };
        
        // Act
        var result = _inventoryService.MoveRoomItemsToBag(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(crawler.Items, Is.Empty);
    }
    
    [Test]
    public void MoveRoomItemsToBag_WhenMoveRequiredFalse_DoesNotMoveItem()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var roomItems = new[] { new InventoryItem { Type = ItemType.Key } };
        var crawler = new Crawler 
        { 
            Id = crawlerId, 
            Bag = Array.Empty<InventoryItem>(),
            Items = roomItems 
        };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        var moveRequests = new[] { new InventoryItem { Type = ItemType.Key, MoveRequired = false } };
        
        // Act
        var result = _inventoryService.MoveRoomItemsToBag(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
        Assert.That(crawler.Items, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void MoveRoomItemsToBag_WhenMultipleItems_MovesOnlyMarkedItems()
    {
        // Arrange
        var crawlerId = Guid.NewGuid();
        var roomItems = new[] 
        { 
            new InventoryItem { Type = ItemType.Key },
            new InventoryItem { Type = ItemType.Key },
            new InventoryItem { Type = ItemType.Key }
        };
        var crawler = new Crawler 
        { 
            Id = crawlerId, 
            Bag = Array.Empty<InventoryItem>(),
            Items = roomItems 
        };
        _mockCrawlerService.Setup(s => s.GetCrawler(crawlerId)).Returns(crawler);
        
        var moveRequests = new[] 
        { 
            new InventoryItem { Type = ItemType.Key, MoveRequired = true },
            new InventoryItem { Type = ItemType.Key, MoveRequired = false },
            new InventoryItem { Type = ItemType.Key, MoveRequired = true }
        };
        
        // Act
        var result = _inventoryService.MoveRoomItemsToBag(crawlerId, moveRequests);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(crawler.Items, Has.Length.EqualTo(1));
    }
    
    #endregion
}
