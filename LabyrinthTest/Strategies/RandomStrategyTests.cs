using Labyrinth.Crawl;
using Labyrinth.Exploration.Strategies;
using Labyrinth.Exploration.Strategies.Implementations;
using Labyrinth.Map;
using Labyrinth.Sys;
using Labyrinth.Tiles;
using Moq;
using static Labyrinth.RandExplorer;

namespace LabyrinthTest.Strategies;

[TestFixture]
public class RandomStrategyTests
{
    private Mock<IEnumRandomizer<Actions>> _mockRandomizer = null!;
    private SharedMap _map = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRandomizer = new Mock<IEnumRandomizer<Actions>>();
        _map = new SharedMap();
    }

    [Test]
    public void Name_ReturnsRandom()
    {
        // Arrange
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        
        // Assert
        Assert.That(strategy.Name, Is.EqualTo("Random"));
    }

    [Test]
    public void DecideNextAction_WhenFacingOutside_ReturnsStop()
    {
        // Arrange
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Outside)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Stop));
    }

    [Test]
    public void DecideNextAction_WhenFacingWall_ReturnsTurnLeft()
    {
        // Arrange
        _mockRandomizer.Setup(r => r.Next()).Returns(Actions.Walk);
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Wall)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft));
    }

    [Test]
    public void DecideNextAction_WhenFacingRoomAndRandomWalk_ReturnsWalk()
    {
        // Arrange
        _mockRandomizer.Setup(r => r.Next()).Returns(Actions.Walk);
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Room)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void DecideNextAction_WhenFacingRoomAndRandomTurnLeft_ReturnsTurnLeft()
    {
        // Arrange
        _mockRandomizer.Setup(r => r.Next()).Returns(Actions.TurnLeft);
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Room)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.TurnLeft));
    }

    [Test]
    public void DecideNextAction_WhenFacingDoorAndRandomWalk_ReturnsWalk()
    {
        // Arrange
        _mockRandomizer.Setup(r => r.Next()).Returns(Actions.Walk);
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        var context = new ExplorationContext(
            (1, 1),
            Direction.North,
            _map,
            typeof(Door)
        );
        
        // Act
        var action = strategy.DecideNextAction(context);
        
        // Assert
        Assert.That(action, Is.EqualTo(ExplorationAction.Walk));
    }

    [Test]
    public void SetTarget_DoesNothing()
    {
        // Arrange
        var strategy = new RandomStrategy(_mockRandomizer.Object);
        
        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => strategy.SetTarget((5, 5)));
        Assert.DoesNotThrow(() => strategy.SetTarget(null));
    }
}
