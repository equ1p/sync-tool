using SyncTool.Logging;

namespace SyncTool.Tests;

public sealed class RecordingLogger : ILogger
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IEnumerable<string> Messages => Entries.Select(entry => entry.Message);

    public void Log(LogLevel level, string message) => Entries.Add((level, message));
}
