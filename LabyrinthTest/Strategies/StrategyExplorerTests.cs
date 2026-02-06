using Labyrinth.Crawl;
using Labyrinth.Exploration;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Items;
using Labyrinth.Map;
using Labyrinth.Tiles;
using Moq;

namespace LabyrinthTest.Strategies;

[TestFixture]
public class StrategyExplorerTests
{
    private Mock<ICrawler> _mockCrawler = null!;
    private Mock<IExplorationStrategy> _mockStrategy = null!;
    private SharedMap _map = null!;

    [SetUp]
    public void SetUp()
    {
        _mockCrawler = new Mock<ICrawler>();
        _mockStrategy = new Mock<IExplorationStrategy>();
        _map = new SharedMap();
        
        // Default crawler setup
        _mockCrawler.SetupGet(c => c.X).Returns(1);
        _mockCrawler.SetupGet(c => c.Y).Returns(1);
        _mockCrawler.SetupGet(c => c.Direction).Returns(Direction.North);
    }

    [Test]
    public void Name_IncludesStrategyName()
    {
        // Arrange
        _mockStrategy.SetupGet(s => s.Name).Returns("TestStrategy");
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Assert
        Assert.That(explorer.Name, Does.Contain("TestStrategy"));
    }

    [Test]
    public async Task ExploreAsync_StopsWhenStrategyReturnsStop()
    {
        // Arrange
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(ExplorationAction.Stop);
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert: Strategy was called once and returned Stop
        _mockStrategy.Verify(s => s.DecideNextAction(It.IsAny<ExplorationContext>()), Times.Once);
    }

    [Test]
    public async Task ExploreAsync_WalksWhenStrategyReturnsWalk()
    {
        // Arrange
        var callCount = 0;
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        _mockCrawler.Setup(c => c.TryWalk(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MyInventory());
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? ExplorationAction.Walk : ExplorationAction.Stop;
            });
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert: TryWalk was called
        _mockCrawler.Verify(c => c.TryWalk(It.IsAny<Inventory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExploreAsync_TurnsLeftWhenStrategyReturnsTurnLeft()
    {
        // Arrange
        var direction = Direction.North;
        _mockCrawler.SetupGet(c => c.Direction).Returns(direction);
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        
        var callCount = 0;
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? ExplorationAction.TurnLeft : ExplorationAction.Stop;
            });
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert: Direction was changed (TurnLeft modifies the direction in place)
        Assert.That(direction.DeltaX, Is.EqualTo(Direction.West.DeltaX));
        Assert.That(direction.DeltaY, Is.EqualTo(Direction.West.DeltaY));
    }

    [Test]
    public async Task ExploreAsync_TurnsRightWhenStrategyReturnsTurnRight()
    {
        // Arrange
        var direction = Direction.North;
        _mockCrawler.SetupGet(c => c.Direction).Returns(direction);
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        
        var callCount = 0;
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? ExplorationAction.TurnRight : ExplorationAction.Stop;
            });
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert: Direction was changed (TurnRight modifies the direction in place)
        Assert.That(direction.DeltaX, Is.EqualTo(Direction.East.DeltaX));
        Assert.That(direction.DeltaY, Is.EqualTo(Direction.East.DeltaY));
    }

    [Test]
    public async Task ExploreAsync_RespectsMaxMoves()
    {
        // Arrange
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(ExplorationAction.TurnLeft);  // Never stops
        
        var maxMoves = 5;
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map, maxMoves);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert: Strategy was called exactly maxMoves times
        _mockStrategy.Verify(s => s.DecideNextAction(It.IsAny<ExplorationContext>()), Times.Exactly(maxMoves));
    }

    [Test]
    public async Task ExploreAsync_ThrowsWhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Room));
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Returns(ExplorationAction.Walk);
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await explorer.ExploreAsync(cts.Token)
        );
    }

    [Test]
    public async Task ExploreAsync_PassesCorrectContextToStrategy()
    {
        // Arrange
        ExplorationContext? capturedContext = null;
        
        _mockCrawler.SetupGet(c => c.X).Returns(5);
        _mockCrawler.SetupGet(c => c.Y).Returns(10);
        _mockCrawler.SetupGet(c => c.Direction).Returns(Direction.East);
        _mockCrawler.Setup(c => c.GetFacingTileTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeof(Wall));
        _mockStrategy.Setup(s => s.DecideNextAction(It.IsAny<ExplorationContext>()))
            .Callback<ExplorationContext>(ctx => capturedContext = ctx)
            .Returns(ExplorationAction.Stop);
        
        var explorer = new StrategyExplorer(_mockCrawler.Object, _mockStrategy.Object, _map);
        
        // Act
        await explorer.ExploreAsync();
        
        // Assert
        Assert.That(capturedContext, Is.Not.Null);
        Assert.That(capturedContext!.CurrentPosition, Is.EqualTo((5, 10)));
        Assert.That(capturedContext.CurrentDirection, Is.EqualTo(Direction.East));
        Assert.That(capturedContext.FacingTileType, Is.EqualTo(typeof(Wall)));
        Assert.That(capturedContext.KnownMap, Is.SameAs(_map));
    }
}
