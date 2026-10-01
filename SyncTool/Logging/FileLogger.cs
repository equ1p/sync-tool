namespace SyncTool.Logging;

public sealed class FileLogger : ILogger, IDisposable
{
    private readonly StreamWriter _writer;

    public FileLogger(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(path, append: true) { AutoFlush = true };
    }

    public void Log(LogLevel level, string message) => _writer.WriteLine(LogEntry.Format(level, message));
    
    public void Dispose() => _writer.Dispose();
}

