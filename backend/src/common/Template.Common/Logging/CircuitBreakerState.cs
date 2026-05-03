namespace Template.Common.Logging;

public sealed class CircuitBreakerState
{
    private readonly Lock _lock = new();
    private int _inFlightRequests;
    private CircuitState _state = CircuitState.Closed;
    private DateTime? _openedAt;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private DateTime _lastCpuCheckTime = DateTime.UtcNow;

    public int IncrementInFlight()
    {
        return Interlocked.Increment(ref _inFlightRequests);
    }

    public int DecrementInFlight()
    {
        return Interlocked.Decrement(ref _inFlightRequests);
    }

    public (TimeSpan lastProcessorTime, DateTime lastCheckTime) SnapshotCpuMeasurement(
        TimeSpan currentProcessorTime, DateTime now)
    {
        lock (_lock)
        {
            var prev = (_lastTotalProcessorTime, _lastCpuCheckTime);
            _lastTotalProcessorTime = currentProcessorTime;
            _lastCpuCheckTime = now;
            return prev;
        }
    }

    // Returns: (shouldReject, retryAfterSeconds, wasHalfOpen, justOpened)
    // justOpened=true when circuit transitions Closed->Open (caller logs the warning)
    public (bool shouldReject, int retryAfterSeconds, bool wasHalfOpen, bool justOpened) EvaluateCircuit(
        double cpuPercent, double memoryPercent,
        double cpuThreshold, double memoryThreshold, int durationSeconds)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            switch (_state)
            {
                case CircuitState.Closed:
                    if (cpuPercent > cpuThreshold || memoryPercent > memoryThreshold)
                    {
                        _state = CircuitState.Open;
                        _openedAt = now;
                        return (true, durationSeconds, false, true);
                    }
                    return (false, 0, false, false);

                case CircuitState.Open:
                    if (_openedAt.HasValue && (now - _openedAt.Value).TotalSeconds >= durationSeconds)
                    {
                        _state = CircuitState.HalfOpen;
                        return (false, 0, true, false);
                    }
                    var remaining = _openedAt.HasValue
                        ? Math.Max(1, (int)Math.Ceiling(durationSeconds - (now - _openedAt.Value).TotalSeconds))
                        : durationSeconds;
                    return (true, remaining, false, false);

                case CircuitState.HalfOpen:
                    return (true, durationSeconds, false, false);

                default:
                    return (false, 0, false, false);
            }
        }
    }

    // Returns: null=not in HalfOpen (no-op), true=transitioned to Closed, false=reopened to Open
    public bool? TryTransitionFromHalfOpen(
        double cpuPercent, double memoryPercent,
        double cpuThreshold, double memoryThreshold)
    {
        lock (_lock)
        {
            if (_state != CircuitState.HalfOpen)
            {
                return null;
            }

            if (cpuPercent <= cpuThreshold && memoryPercent <= memoryThreshold)
            {
                _state = CircuitState.Closed;
                _openedAt = null;
                return true;
            }

            _state = CircuitState.Open;
            _openedAt = DateTime.UtcNow;
            return false;
        }
    }
}
