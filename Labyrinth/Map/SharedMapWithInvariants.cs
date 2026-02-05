using Labyrinth.Tiles;
using System.Collections.Concurrent;

namespace Labyrinth.Map;

/// <summary>
/// Extended SharedMap with invariant enforcement and conflict logging (Issue #12)
/// </summary>
public class SharedMapWithInvariants : SharedMap
{
    private readonly ConcurrentBag<InvariantViolation> _invariantViolations = new();
    private readonly ConcurrentBag<ConflictLog> _conflictLogs = new();
    private int _writeAttempts;

    public IReadOnlyCollection<InvariantViolation> InvariantViolations => _invariantViolations;
    public IReadOnlyCollection<ConflictLog> ConflictLogs => _conflictLogs;
    public int WriteAttempts => _writeAttempts;

    /// <summary>
    /// Set tile with invariant checking and conflict resolution
    /// Returns false if invariant was violated and tile was NOT updated
    /// </summary>
    public bool SetTileWithInvariant((int x, int y) position, Tile newTile)
    {
        Interlocked.Increment(ref _writeAttempts);

        var existingTile = GetTile(position);

        // INVARIANT 1: Known tile cannot become Unknown
        if (existingTile != null && newTile is Unknown)
        {
            _invariantViolations.Add(new InvariantViolation(
                position,
                $"Cannot set {existingTile.GetType().Name} back to Unknown",
                DateTime.UtcNow
            ));
            return false;
        }

        if (existingTile != null && existingTile.GetType() != newTile.GetType())
        {
            _conflictLogs.Add(new ConflictLog(
                position,
                existingTile.GetType().Name,
                newTile.GetType().Name,
                "NewInfoPriority", // Resolution rule: most recent info wins
                DateTime.UtcNow
            ));
        }

        SetTile(position, newTile);
        return true;
    }

    /// <summary>
    /// Try to set a position back to unknown (should fail for known tiles)
    /// </summary>
    public bool TrySetUnknown((int x, int y) position)
    {
        var existingTile = GetTile(position);

        if (existingTile != null)
        {
            _invariantViolations.Add(new InvariantViolation(
                position,
                "Attempted to set known tile to Unknown",
                DateTime.UtcNow
            ));
            return false;
        }

        return true;
    }
}

/// <summary>
/// Immutable record of an invariant violation (e.g., known tile reverting to Unknown)
/// </summary>
public sealed record InvariantViolation(
    (int x, int y) Position,
    string Reason,
    DateTime Timestamp
);

/// <summary>
/// Immutable record of a tile type conflict with resolution strategy
/// </summary>
public sealed record ConflictLog(
    (int x, int y) Position,
    string PreviousType,
    string NewType,
    string Resolution,
    DateTime Timestamp
);
