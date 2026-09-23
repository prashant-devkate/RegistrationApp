using System.Threading.Channels;

namespace RegistrationApp.Services;

/// <summary>
/// Snapshot of the reconciliation runner's state, safe to hand to a view
/// </summary>
public record ReconciliationRunState(
    bool IsRunning,
    DateTime? LastRunAt,
    string? LastMessage,
    bool LastRunFailed,
    string? LastTrigger);

/// <summary>
/// Coordinates payment reconciliation runs.
///
/// Requests (from the dashboard button or the daily schedule) are queued rather than
/// executed inline, so a run is owned by the host and cannot be cut short by a user
/// navigating away from the page. Only one run is ever queued or in flight at a time.
/// </summary>
public class PaymentReconciliationRunner
{
    // Capacity of one: while a run is queued, further requests are rejected rather
    // than piling up. Combined with the IsRunning check this gives single-flight.
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait });

    private readonly object _stateLock = new();

    private bool _isRunning;
    private DateTime? _lastRunAt;
    private string? _lastMessage;
    private bool _lastRunFailed;
    private string? _lastTrigger;

    /// <summary>
    /// Reader consumed by the background worker
    /// </summary>
    public ChannelReader<string> Queue => _queue.Reader;

    /// <summary>
    /// Requests a run. Returns false when a run is already queued or in progress,
    /// which the caller should surface rather than silently ignore.
    /// </summary>
    public bool TryEnqueue(string trigger)
    {
        lock (_stateLock)
        {
            if (_isRunning)
            {
                return false;
            }
        }

        return _queue.Writer.TryWrite(trigger);
    }

    /// <summary>
    /// Current state snapshot
    /// </summary>
    public ReconciliationRunState GetState()
    {
        lock (_stateLock)
        {
            return new ReconciliationRunState(_isRunning, _lastRunAt, _lastMessage, _lastRunFailed, _lastTrigger);
        }
    }

    internal void MarkStarted(string trigger)
    {
        lock (_stateLock)
        {
            _isRunning = true;
            _lastTrigger = trigger;
        }
    }

    internal void MarkCompleted(DateTime completedAt, string message, bool failed)
    {
        lock (_stateLock)
        {
            _isRunning = false;
            _lastRunAt = completedAt;
            _lastMessage = message;
            _lastRunFailed = failed;
        }
    }
}