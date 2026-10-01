namespace SyncTool.Cli;

public sealed record SyncOptions(
    string SourcePath,
    string ReplicaPath,
    TimeSpan Interval,
    string LogFilePath,
    bool RunOnce
);
