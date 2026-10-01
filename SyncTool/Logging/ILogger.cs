namespace SyncTool.Logging;

public interface ILogger
{
    void Log(LogLevel level, string message);
    void Info(string message) => Log(LogLevel.Info, message);
    void Warning(string message) => Log(LogLevel.Warning, message);
    void Error(string message) => Log(LogLevel.Error, message);
}

