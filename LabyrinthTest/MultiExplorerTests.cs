using Labyrinth.Crawl;
using Labyrinth.Map;
using Labyrinth.Exploration;
using Moq;

namespace LabyrinthTest;

/// <summary>
/// Tests for MultiExplorer orchestrator (Issue #12)
/// Tests concurrent exploration with shared map
/// </summary>
[TestFixture]
public class MultiExplorerTests
{
    [Test]
    public async Task StartAsync_WithTwoExplorers_ExecutesSimultaneously()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var crawler1 = new Mock<ICrawler>();
        var crawler2 = new Mock<ICrawler>();
        
        var explorer1 = new TestExplorer("Explorer1", crawler1.Object, sharedMap);
        var explorer2 = new TestExplorer("Explorer2", crawler2.Object, sharedMap);
        
        var multiExplorer = new MultiExplorer(new[] { explorer1, explorer2 }, sharedMap);

        // Act
        await multiExplorer.StartAsync();

        // Assert
        Assert.That(explorer1.HasExecuted, Is.True);
        Assert.That(explorer2.HasExecuted, Is.True);
        var stats = multiExplorer.GetAggregatedStats();
        Assert.That(stats.CompletedExplorers, Is.EqualTo(2));
    }

    [Test]
    public async Task ConcurrentWrites_ToSharedMap_MaintainConsistency()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var explorers = Enumerable.Range(0, 10)
            .Select(i => CreateExplorerWithPosition($"Explorer{i}", sharedMap, (5, 5)))
            .ToArray();
        
        var multiExplorer = new MultiExplorer(explorers, sharedMap);

        // Act
        await multiExplorer.StartAsync();
        await multiExplorer.WaitForCompletionAsync(TimeSpan.FromSeconds(5));

        // Assert
        var tile = sharedMap.GetTile((5, 5));
        Assert.That(tile, Is.Not.Null, "Position should have been written");
        Assert.That(sharedMap.TileCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task CancelAsync_StopsAllExplorersGracefully()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var cts = new CancellationTokenSource();
        
        var explorers = Enumerable.Range(0, 3)
            .Select(i => new LongRunningExplorer($"Explorer{i}", sharedMap))
            .ToArray();
        
        var multiExplorer = new MultiExplorer(explorers, sharedMap);

        // Act
        var runTask = multiExplorer.StartAsync(cts.Token);
        await Task.Delay(100); // Let explorers start
        cts.Cancel();
        
        // Assert - Wait for task to complete with cancellation
        try
        {
            await runTask;
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        Assert.That(explorers.All(e => e.WasCancelled), Is.True);
    }

    [Test]
    public async Task GetStats_AggregatesResultsFromAllExplorers()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var explorers = new[]
        {
            new StatsExplorer("Explorer1", sharedMap, stepsToSimulate: 100),
            new StatsExplorer("Explorer2", sharedMap, stepsToSimulate: 150),
            new StatsExplorer("Explorer3", sharedMap, stepsToSimulate: 75)
        };
        
        var multiExplorer = new MultiExplorer(explorers, sharedMap);

        // Act
        await multiExplorer.StartAsync();
        await multiExplorer.WaitForCompletionAsync(TimeSpan.FromSeconds(5));
        var stats = multiExplorer.GetAggregatedStats();

        // Assert
        Assert.That(stats.TotalExplorers, Is.EqualTo(3));
        Assert.That(stats.CompletedExplorers, Is.EqualTo(3));
        Assert.That(stats.TotalSteps, Is.EqualTo(325));
        Assert.That(stats.ExploredCells, Is.EqualTo(sharedMap.TileCount));
    }

    [Test]
    public async Task DeterministicConcurrency_UsingBarriers()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var barrier = new Barrier(3);
        
        var explorers = Enumerable.Range(0, 3)
            .Select(i => new BarrierExplorer($"Explorer{i}", sharedMap, barrier))
            .ToArray();
        
        var multiExplorer = new MultiExplorer(explorers, sharedMap);

        // Act
        await multiExplorer.StartAsync();
        await multiExplorer.WaitForCompletionAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.That(explorers.All(e => e.ReachedBarrier), Is.True);
        Assert.That(explorers.All(e => e.HasExecuted), Is.True);
    }

    [Test]
    public async Task ResourceConflict_MultipleExplorers_OneSucceedsOthersRetry()
    {
        // Arrange
        var sharedMap = new SharedMap();
        var sharedResource = new SharedResource("key1");
        
        var explorers = Enumerable.Range(0, 3)
            .Select(i => new ResourceSeekingExplorer($"Explorer{i}", sharedMap, sharedResource))
            .ToArray();
        
        var multiExplorer = new MultiExplorer(explorers, sharedMap);

        // Act
        await multiExplorer.StartAsync();
        await multiExplorer.WaitForCompletionAsync(TimeSpan.FromSeconds(5));

        // Assert
        var successful = explorers.Count(e => e.AcquiredResource);
        var failed = explorers.Count(e => e.FailedToAcquire);
        
        Assert.That(successful, Is.EqualTo(1), "Only one explorer should acquire the resource");
        Assert.That(failed, Is.EqualTo(2), "Other explorers should fail");
    }

    // Helper method
    private TestExplorer CreateExplorerWithPosition(string name, SharedMap map, (int x, int y) position)
    {
        var crawler = new Mock<ICrawler>();
        return new TestExplorer(name, crawler.Object, map, position);
    }
}
