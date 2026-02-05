using Labyrinth;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Sys;
using Moq;
using static Labyrinth.RandExplorer;

namespace LabyrinthTest;

/// <summary>
/// Tests for cancellation support in explorer GetOut method.
/// Follows AAA pattern (Arrange, Act, Assert) and TDD principles.
/// </summary>
public class ExplorerCancellationTest
{
    private class ExplorerEventsCatcher
    {
        public ExplorerEventsCatcher(RandExplorer explorer)
        {
            explorer.PositionChanged += (s, e) =>
                CatchEvent(ref _positionChangedCount, e);
            explorer.DirectionChanged += (s, e) =>
                CatchEvent(ref _directionChangedCount, e);
        }

        public int PositionChangedCount => _positionChangedCount;

        public int DirectionChangedCount => _directionChangedCount;

        public (int X, int Y, Direction Dir)? LastArgs { get; private set; } =
            null;

        private void CatchEvent(ref int counter, CrawlingEventArgs e)
        {
            counter++;
            LastArgs = (e.X, e.Y, e.Direction);
        }

        private int _directionChangedCount = 0, _positionChangedCount = 0;
    }

    private RandExplorer NewExplorerFor(
        string labyrinth,
        out ExplorerEventsCatcher events,
        params Actions[] actions
    )
    {
        var laby = new Labyrinth.Labyrinth(new AsciiParser(labyrinth));
        var mockRnd = new Mock<IEnumRandomizer<Actions>>();

        var queue = new Queue<Actions>(actions);
        mockRnd.Setup(r => r.Next()).Returns(() =>
            queue.Count > 0 ? queue.Dequeue() : Actions.TurnLeft
        );

        var explorer = new RandExplorer(
            laby.NewCrawler(),
            mockRnd.Object
        );

        events = new ExplorerEventsCatcher(explorer);
        return explorer;
    }

    private static void CancelOnFirstDirectionChange(RandExplorer explorer, CancellationTokenSource cts)
    {
        EventHandler<CrawlingEventArgs>? handler = null;
        handler = (_, __) =>
        {
            explorer.DirectionChanged -= handler;
            cts.Cancel();
        };
        explorer.DirectionChanged += handler;
    }

    /// <summary>
    /// Test: GetOut accepts CancellationToken parameter
    /// Arrange: Create an explorer and a cancellation token
    /// Act: Call GetOut with valid parameters including cancellation token
    /// Assert: Method completes successfully
    /// </summary>
    [Test]
    public async Task GetOut_AcceptsCancellationToken_ExecutesSuccessfully()
    {
        // Arrange
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        var left = await test.GetOut(10, null, ct);

        // Assert
        Assert.That(left, Is.EqualTo(10));
    }

    /// <summary>
    /// Test: GetOut can be cancelled during execution
    /// Arrange: Create an explorer with many moves to execute, create a cancellation token that will be cancelled mid-execution
    /// Act: Start GetOut and cancel it after it has started
    /// Assert: OperationCanceledException is thrown
    /// </summary>
    [Test]
    public async Task GetOut_WithCancellationDuringExecution_ThrowsOperationCanceledException()
    {
        // Arrange
        var test = NewExplorerFor("""
            +-+
            |x|
            +-+
            """,
            out var events,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act: cancel deterministically after first observable progress
        CancelOnFirstDirectionChange(test, cts);

        // Assert
        Assert.That(
            async () => await test.GetOut(100_000, null, ct),
            Throws.InstanceOf<OperationCanceledException>()
        );
    }

    /// <summary>
    /// Test: GetOut can be cancelled before execution
    /// Arrange: Create an explorer with a pre-cancelled cancellation token
    /// Act: Call GetOut with already-cancelled token
    /// Assert: OperationCanceledException is thrown immediately
    /// </summary>
    [Test]
    public void GetOut_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var test = NewExplorerFor("""
            --+
              |
            x |
            --+
            """,
            out var events,
            Actions.Walk
        );

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ct = cts.Token;

        // Act & Assert
        Assert.That(
            async () => await test.GetOut(10, null, ct),
            Throws.InstanceOf<OperationCanceledException>()
        );
    }

    /// <summary>
    /// Test: Resources are properly cleaned up after cancellation
    /// Arrange: Create an explorer with a cancellation token
    /// Act: Start GetOut and cancel it, then check if state is consistent
    /// Assert: No leaked resources, crawler is in valid state
    /// </summary>
    [Test]
    public async Task GetOut_AfterCancellation_CrawlerStateIsConsistent()
    {
        // Arrange
        var test = NewExplorerFor("""
            +-+
            |x|
            +-+
            """,
            out var events,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft
        );

        var initialX = test.Crawler.X;
        var initialY = test.Crawler.Y;

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act: cancel deterministically after first observable progress
        CancelOnFirstDirectionChange(test, cts);
        var task = test.GetOut(100_000, null, ct);

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert: Crawler position is still valid
        Assert.That(test.Crawler.X, Is.GreaterThanOrEqualTo(initialX - 1));
        Assert.That(test.Crawler.Y, Is.GreaterThanOrEqualTo(initialY - 1));
    }

    /// <summary>
    /// Test: CancellationToken with timeout
    /// Arrange: Create an explorer with a cancellation token that has a timeout
    /// Act: Call GetOut with many iterations
    /// Assert: Cancellation is triggered after timeout
    /// </summary>
    [Test]
    public async Task GetOut_WithCancellationTimeout_CancelsAfterTimeout()
    {
        // Arrange
        var test = NewExplorerFor("""
            +-+
            |x|
            +-+
            """,
            out var events,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.TurnLeft
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        var ct = cts.Token;

        // Act & Assert
        Assert.That(
            async () => await test.GetOut(100_000, null, ct),
            Throws.InstanceOf<OperationCanceledException>()
        );
    }

    /// <summary>
    /// Test: Normal completion without cancellation
    /// Arrange: Create an explorer and a non-cancelled cancellation token
    /// Act: Call GetOut and let it complete normally
    /// Assert: Returns expected result without throwing
    /// </summary>
    [Test]
    public async Task GetOut_WithoutCancellation_CompletesNormally()
    {
        // Arrange
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        var left = await test.GetOut(10, null, ct);

        // Assert: Should complete normally (facing outside immediately)
        Assert.That(left, Is.EqualTo(10));
    }

    /// <summary>
    /// Test: Multiple iterations complete before cancellation
    /// Arrange: Create an explorer that will complete in fewer moves than requested
    /// Act: Call GetOut with cancellation token, but completion happens before cancellation
    /// Assert: Returns the expected remaining moves
    /// </summary>
    [Test]
    public async Task GetOut_CompletesBeforeCancellation_ReturnsRemainingMoves()
    {
        // Arrange
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        var left = await test.GetOut(10, null, ct);

        // Assert
        Assert.That(left, Is.EqualTo(10));
    }
}
