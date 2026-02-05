using Labyrinth.Crawl;
using Labyrinth.Map;
using Labyrinth.Tiles;
using Labyrinth.Exploration;

namespace LabyrinthTest;

public class TestExplorer : IExplorer
{
    public string Name { get; }
    public bool HasExecuted { get; private set; }
    private readonly ICrawler _crawler;
    private readonly SharedMap _sharedMap;
    private readonly (int x, int y)? _targetPosition;

    public TestExplorer(string name, ICrawler crawler, SharedMap sharedMap, (int x, int y)? targetPosition = null)
    {
        Name = name;
        _crawler = crawler;
        _sharedMap = sharedMap;
        _targetPosition = targetPosition;
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        HasExecuted = true;
        
        if (_targetPosition.HasValue)
        {
            _sharedMap.SetTile(_targetPosition.Value, new Room());
        }
    }
}

public class LongRunningExplorer : IExplorer
{
    public string Name { get; }
    public bool WasCancelled { get; private set; }
    private readonly SharedMap _sharedMap;

    public LongRunningExplorer(string name, SharedMap sharedMap)
    {
        Name = name;
        _sharedMap = sharedMap;
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            for (int i = 0; i < 1000; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _sharedMap.SetTile((i, i), new Room());
                await Task.Delay(10, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            WasCancelled = true;
            throw;
        }
    }
}

public class StatsExplorer : IStepsTrackingExplorer
{
    public string Name { get; }
    public bool HasExecuted { get; private set; }
    public int StepsExecuted { get; private set; }
    private readonly SharedMap _sharedMap;
    private readonly int _stepsToSimulate;

    public StatsExplorer(string name, SharedMap sharedMap, int stepsToSimulate)
    {
        Name = name;
        _sharedMap = sharedMap;
        _stepsToSimulate = stepsToSimulate;
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < _stepsToSimulate; i++)
        {
            _sharedMap.SetTile((i, i), new Room());
            StepsExecuted++;
            await Task.Yield();
        }
        HasExecuted = true;
    }
}

public class BarrierExplorer : IExplorer
{
    public string Name { get; }
    public bool HasExecuted { get; private set; }
    public bool ReachedBarrier { get; private set; }
    private readonly SharedMap _sharedMap;
    private readonly Barrier _barrier;

    public BarrierExplorer(string name, SharedMap sharedMap, Barrier barrier)
    {
        Name = name;
        _sharedMap = sharedMap;
        _barrier = barrier;
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _sharedMap.SetTile((0, 0), new Room());
            _barrier.SignalAndWait(cancellationToken);
            ReachedBarrier = true;
        }, cancellationToken);
        
        HasExecuted = true;
    }
}

public class ResourceSeekingExplorer : IExplorer
{
    public string Name { get; }
    public bool AcquiredResource { get; private set; }
    public bool FailedToAcquire { get; private set; }
    private readonly SharedMap _sharedMap;
    private readonly SharedResource _resource;

    public ResourceSeekingExplorer(string name, SharedMap sharedMap, SharedResource resource)
    {
        Name = name;
        _sharedMap = sharedMap;
        _resource = resource;
    }

    public async Task ExploreAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        if (_resource.TryAcquire(Name))
        {
            AcquiredResource = true;
            await Task.Delay(50, cancellationToken);
        }
        else
        {
            FailedToAcquire = true;
            await Task.Delay(10, cancellationToken);
        }
    }
}

public class SharedResource
{
    private string? _acquiredBy;
    private readonly object _lock = new();

    public SharedResource(string resourceId)
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }

    public bool TryAcquire(string explorerId)
    {
        lock (_lock)
        {
            if (_acquiredBy == null)
            {
                _acquiredBy = explorerId;
                return true;
            }
            return false;
        }
    }
}
