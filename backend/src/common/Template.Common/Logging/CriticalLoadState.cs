namespace Template.Common.Logging;

public sealed class CriticalLoadState : IDisposable
{
    private readonly Lock _syncRoot = new();
    private CancellationTokenSource _criticalStateCts = new();
    private Timer? _resetTimer;
    private DateTimeOffset _criticalUntilUtc = DateTimeOffset.MinValue;
    private string? _reason;

    public bool IsActive
    {
        get
        {
            lock (_syncRoot)
            {
                return DateTimeOffset.UtcNow < _criticalUntilUtc;
            }
        }
    }

    public string? Reason
    {
        get
        {
            lock (_syncRoot)
            {
                return _reason;
            }
        }
    }

    public CancellationToken CreateLinkedToken(CancellationToken requestAborted, out CancellationTokenSource linkedCts)
    {
        lock (_syncRoot)
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted, _criticalStateCts.Token);
            return linkedCts.Token;
        }
    }

    public void EnterCriticalState(string reason, TimeSpan duration)
    {
        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var candidateUntil = now.Add(duration);

            if (candidateUntil > _criticalUntilUtc)
            {
                _criticalUntilUtc = candidateUntil;
            }

            _reason = reason;

            if (!_criticalStateCts.IsCancellationRequested)
            {
                _criticalStateCts.Cancel();
            }

            var dueTime = _criticalUntilUtc - now;
            if (dueTime < TimeSpan.Zero)
            {
                dueTime = TimeSpan.Zero;
            }

            _resetTimer?.Dispose();
            _resetTimer = new Timer(_ => ResetIfExpired(), null, dueTime, Timeout.InfiniteTimeSpan);
        }
    }

    private void ResetIfExpired()
    {
        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < _criticalUntilUtc)
            {
                var dueTime = _criticalUntilUtc - now;
                _resetTimer?.Dispose();
                _resetTimer = new Timer(_ => ResetIfExpired(), null, dueTime, Timeout.InfiniteTimeSpan);
                return;
            }

            _criticalStateCts.Dispose();
            _criticalStateCts = new CancellationTokenSource();
            _criticalUntilUtc = DateTimeOffset.MinValue;
            _reason = null;

            _resetTimer?.Dispose();
            _resetTimer = null;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _resetTimer?.Dispose();
            _criticalStateCts.Dispose();
        }
    }
}
