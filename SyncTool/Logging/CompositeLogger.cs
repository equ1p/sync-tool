namespace SyncTool.Logging;
public sealed class CompositeLogger : ILogger, IDisposable
{
    private readonly IReadOnlyList<ILogger> _sinks;

    public CompositeLogger(params ILogger[] sinks) => _sinks = sinks;

    public void Log(LogLevel level, string message)
    {
        foreach (ILogger sink in _sinks)
        {
            sink.Log(level, message);
        }
    }

    public void Dispose()
    {
        foreach (ILogger sink in _sinks)
        {
            (sink as IDisposable)?.Dispose();
        }
    }
}

