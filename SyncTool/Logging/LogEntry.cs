namespace SyncTool.Logging;

public static class LogEntry
{
    public static string Format(LogLevel level, string message) =>
        Format(DateTimeOffset.Now, level, message);

    public static string Format(DateTimeOffset timestamp, LogLevel level, string message)
    {
        string label = level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => level.ToString().ToUpperInvariant()
        };

        return FormattableString.Invariant($"{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{label}] {message}");
    }
}
