namespace voks.server.records;

public class ApplicationTermination
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly CancellationTokenSource _mainWindowClosed = new();
    public CancellationToken GetCancellationToken() => _mainWindowClosed.Token;

    public ApplicationTermination(IHostApplicationLifetime applicationLifetime)
    {
        _lifetime = applicationLifetime;
    }

    public void Stop()
    {
        _mainWindowClosed.Cancel();
        _lifetime.StopApplication();
    }
}
