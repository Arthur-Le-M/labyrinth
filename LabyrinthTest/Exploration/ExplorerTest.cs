using Labyrinth.Exploration;
using Labyrinth.Crawl;
using Labyrinth.Tiles;
using Moq;

namespace LabyrinthTest.Exploration;

[TestFixture(Description = "Unit tests for the Explorer class controlling random labyrinth exploration.")]
public class ExplorerTest
{
    [Test]
    public void Constructor_Throws_WhenCrawlerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Explorer(null!));
    }

    [Test]
    public void GetOut_DoesNothing_WhenStepsIsZeroOrNegative()
    {
        var mockCrawler = new Mock<ICrawler>();
        var explorer = new Explorer(mockCrawler.Object);

        explorer.GetOut(0);
        explorer.GetOut(-5);

        mockCrawler.VerifyNoOtherCalls();
    }

    [Test]
    public void GetOut_Stops_WhenOutsideReached()
    {
        var mockCrawler = new Mock<ICrawler>();

        mockCrawler.SetupSequence(c => c.FacingTile)
            .Returns(new Room())
            .Returns(Outside.Singleton);

        mockCrawler.Setup(c => c.Direction).Returns(Direction.North);

        var fixedRandom = new Mock<Random>();
        fixedRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        var explorer = new Explorer(mockCrawler.Object, fixedRandom.Object);

        explorer.GetOut(10);

        mockCrawler.Verify(c => c.Walk(), Times.Once);
    }

    [Test]
    public void GetOut_CallsWalkOrTurn_UpToN_Steps()
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.FacingTile).Returns(new Room());

        var fixedRandom = new Mock<Random>();
        fixedRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        var explorer = new Explorer(mockCrawler.Object, fixedRandom.Object);
        explorer.GetOut(5);

        mockCrawler.Verify(c => c.Walk(), Times.Exactly(5));
    }

    [Test]
    public void GetOut_PerformsRandomMix_OfWalkAndTurn()
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.FacingTile).Returns(new Room());
        mockCrawler.Setup(c => c.Direction).Returns(Direction.North);

        var fakeRandom = new Mock<Random>();
        fakeRandom.SetupSequence(r => r.Next(It.IsAny<int>()))
            .Returns(0).Returns(1).Returns(2).Returns(0).Returns(1);

        var explorer = new Explorer(mockCrawler.Object, fakeRandom.Object);
        explorer.GetOut(5);

        mockCrawler.Verify(c => c.Walk(), Times.AtLeastOnce);
        mockCrawler.Verify(c => c.Direction, Times.AtLeast(1));
    }

    [Test]
    public void GetOut_StopsImmediately_WhenFirstTileIsOutside()
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.FacingTile).Returns(Outside.Singleton);

        var explorer = new Explorer(mockCrawler.Object);
        explorer.GetOut(10);

        mockCrawler.Verify(c => c.Walk(), Times.Never);
    }

    [Test]
    public void GetOut_UsesProvidedRandom()
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.FacingTile).Returns(new Room());
        mockCrawler.Setup(c => c.Direction).Returns(Direction.North);

        var customRandom = new Mock<Random>();
        customRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(2);

        var explorer = new Explorer(mockCrawler.Object, customRandom.Object);

        explorer.GetOut(3);

        customRandom.Verify(r => r.Next(It.IsAny<int>()), Times.Exactly(3));
    }

}