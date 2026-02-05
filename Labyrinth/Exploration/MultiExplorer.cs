using Labyrinth.Map;

namespace Labyrinth.Exploration;

/// <summary>
/// Orchestrator for concurrent exploration with multiple explorers (Issue #12)
/// Manages parallel execution and shared map coordination
/// Respects SOLID principles: DIP (depends on IExplorer, ISharedMap), SRP (focused on orchestration)
/// </summary>
public class MultiExplorer
{
    private readonly IEnumerable<IExplorer> _explorers;
    private readonly ISharedMap _sharedMap;
    private readonly List<Task> _runningTasks = new();
    private readonly ExplorationStats _stats = new();
    private CancellationTokenSource? _cts;

    public MultiExplorer(IEnumerable<IExplorer> explorers, ISharedMap sharedMap)
    {
        _explorers = explorers ?? throw new ArgumentNullException(nameof(explorers));
        _sharedMap = sharedMap ?? throw new ArgumentNullException(nameof(sharedMap));
    }

    public int ActiveExplorersCount => _runningTasks.Count(t => !t.IsCompleted);

    public void RecordResourceConflict()
    {
        _stats.IncrementResourceConflicts();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var explorer in _explorers)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    _stats.IncrementTotalExplorers();
                    await explorer.ExploreAsync(_cts.Token);
                    _stats.IncrementCompletedExplorers();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _stats.IncrementFailedExplorers();
                    // TODO: Use proper logging instead of Console.WriteLine
                    Console.WriteLine($"Explorer {explorer.Name} failed: {ex.Message}");
                }
            }, _cts.Token);

            _runningTasks.Add(task);
        }

        await Task.WhenAll(_runningTasks);
    }

    public async Task WaitForCompletionAsync(TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completionTask = Task.WhenAll(_runningTasks);

        var completed = await Task.WhenAny(completionTask, timeoutTask);

        if (completed == timeoutTask)
        {
            _cts?.Cancel();
        }
    }

    public ExplorationStats GetAggregatedStats()
    {
        _stats.UpdateExploredCells(_sharedMap.TileCount);
        
        int totalSteps = 0;
        foreach (var explorer in _explorers)
        {
            if (explorer is IStepsTrackingExplorer trackingExplorer)
            {
                totalSteps += trackingExplorer.StepsExecuted;
            }
        }
        _stats.UpdateTotalSteps(totalSteps);
        
        return _stats;
    }
}

public class ExplorationStats
{
    private int _resourceConflicts;
    private int _totalExplorers;
    private int _completedExplorers;
    private int _failedExplorers;
    private int _totalSteps;
    private int _exploredCells;

    public int TotalExplorers => _totalExplorers;
    public int CompletedExplorers => _completedExplorers;
    public int FailedExplorers => _failedExplorers;
    public int TotalSteps => _totalSteps;
    public int ExploredCells => _exploredCells;
    public int ResourceConflicts => _resourceConflicts;

    internal void IncrementTotalExplorers() => Interlocked.Increment(ref _totalExplorers);
    internal void IncrementCompletedExplorers() => Interlocked.Increment(ref _completedExplorers);
    internal void IncrementFailedExplorers() => Interlocked.Increment(ref _failedExplorers);
    internal void IncrementResourceConflicts() => Interlocked.Increment(ref _resourceConflicts);
    internal void UpdateExploredCells(int value) => _exploredCells = value;
    internal void UpdateTotalSteps(int value) => _totalSteps = value;
}
