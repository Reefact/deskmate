namespace DeskMate.Core;

/// <summary>At most one running operation of a kind: starting a new one cancels the previous one.</summary>
internal sealed class OperationSlot
{
    private readonly Lock _gate = new();
    private CancellationTokenSource? _current;

    public CancellationTokenSource Begin(CancellationToken parent)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(parent);
        lock (_gate)
        {
            _current?.Cancel();
            _current = cts;
        }
        return cts;
    }

    public void Cancel()
    {
        lock (_gate)
            _current?.Cancel();
    }

    public void End(CancellationTokenSource cts)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_current, cts))
                _current = null;
        }
        cts.Dispose();
    }
}
