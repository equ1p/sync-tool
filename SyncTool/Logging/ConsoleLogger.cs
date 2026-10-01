namespace SyncTool.Logging;
public sealed class ConsoleLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        TextWriter writer = level == LogLevel.Info ? Console.Out : Console.Error;
        writer.WriteLine(LogEntry.Format(level, message));
    }
}
