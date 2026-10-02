using SyncTool.Logging;

namespace SyncTool.Sync;

public sealed class FolderSynchronizer
{
    private static readonly EnumerationOptions RecursiveEnumeration = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.None,
        IgnoreInaccessible = true
    };

    private readonly string _sourceRoot;
    private readonly string _replicaRoot;
    private readonly ILogger _logger;

    public FolderSynchronizer(string sourceRoot, string replicaRoot, ILogger logger)
    {
        _sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceRoot));
        _replicaRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(replicaRoot));
        _logger = logger;
    }

    public SyncSummary Synchronize(CancellationToken cancellationToken = default)
    {
        var summary = new SyncSummary();

        HashSet<string> sourceDirectories = EnumerateRelative(_sourceRoot, EntryKind.Directory);
        HashSet<string> sourceFiles = EnumerateRelative(_sourceRoot, EntryKind.File);

        EnsureReplicaRootExists(summary);

        HashSet<string> replicaDirectories = EnumerateRelative(_replicaRoot, EntryKind.Directory);
        HashSet<string> replicaFiles = EnumerateRelative(_replicaRoot, EntryKind.File);

        DeleteExtraFiles(replicaFiles, sourceFiles, summary, cancellationToken);
        DeleteExtraDirectories(replicaDirectories, sourceDirectories, summary, cancellationToken);
        
        CreateMissingDirectories(sourceDirectories, summary, cancellationToken);
        CopyNewAndChangedFiles(sourceFiles, summary, cancellationToken);

        return summary;
    }

    private enum EntryKind
    {
        File,
        Directory
    }

    private static HashSet<string> EnumerateRelative(string root, EntryKind kind)
    {
        var entries = new HashSet<string>(PathComparer.Comparer);

        if (!Directory.Exists(root))
        {
            return entries;
        }

        IEnumerable<string> found = kind == EntryKind.File
            ? Directory.EnumerateFiles(root, "*", RecursiveEnumeration)
            : Directory.EnumerateDirectories(root, "*", RecursiveEnumeration);

        foreach (var entry in found)
        {
            entries.Add(Path.GetRelativePath(root, entry));
        }

        return entries;
    }

    private void EnsureReplicaRootExists(SyncSummary summary)
    {
        if (Directory.Exists(_replicaRoot))
        {
            return;
        }

        Directory.CreateDirectory(_replicaRoot);
        summary.DirectoriesCreated++;
        _logger.Info($"Created directory: {_replicaRoot}");
    }

    private void DeleteExtraFiles(
        HashSet<string> replicaFiles,
        HashSet<string> sourceFiles,
        SyncSummary summary,
        CancellationToken cancellationToken)
    {
        foreach (string relativePath in replicaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceFiles.Contains(relativePath))
            {
                continue;
            }

            string fullPath = Path.Combine(_replicaRoot, relativePath);

            Execute(summary, $"delete file '{fullPath}'", () =>
            {
                ClearReadOnly(fullPath);
                File.Delete(fullPath);
                summary.FilesDeleted++;
                _logger.Info($"Deleted file: {fullPath}");
            });
        }
    }

    private void DeleteExtraDirectories(
        HashSet<string> replicaDirectories,
        HashSet<string> sourceDirectories,
        SyncSummary summary,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> extra = replicaDirectories
            .Where(relativePath => !sourceDirectories.Contains(relativePath))
            .OrderByDescending(relativePath => Depth(relativePath));

        foreach (string relativePath in extra)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullPath = Path.Combine(_replicaRoot, relativePath);

            if (!Directory.Exists(fullPath))
            {
                continue;
            }

            Execute(summary, $"delete directory '{fullPath}'", () =>
                {
                    Directory.Delete(fullPath, recursive: true);
                    summary.DirectoriesDeleted++;
                    _logger.Info($"Deleted directory: {fullPath}");
                });
        }
    }

    private void CreateMissingDirectories(
        HashSet<string> sourceDirectories,
        SyncSummary summary,
        CancellationToken cancellationToken)
    {
        foreach (string relativePath in sourceDirectories.OrderBy(Depth))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fullPath = Path.Combine(_replicaRoot, relativePath);

            if (Directory.Exists(fullPath))
            {
                continue;
            }

            Execute(summary, $"create directory '{fullPath}'", () =>
            {
                Directory.CreateDirectory(fullPath);
                summary.DirectoriesCreated++;
                _logger.Info($"Created directory: {fullPath}");
            });
        }
    }

    private void CopyNewAndChangedFiles(
        HashSet<string> sourceFiles,
        SyncSummary summary,
        CancellationToken cancellationToken)
    {
        foreach (string relativePath in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sourcePath = Path.Combine(_sourceRoot, relativePath);
            string replicaPath = Path.Combine(_replicaRoot, relativePath);

            Execute(summary, $"copy '{sourcePath}' to '{replicaPath}'", () =>
            {
                bool alreadyExists = File.Exists(replicaPath);

                if (alreadyExists && FileComparer.HaveSameContent(sourcePath, replicaPath))
                {
                    return;
                }

                if (alreadyExists)
                {
                    ClearReadOnly(replicaPath);
                }

                File.Copy(sourcePath, replicaPath, overwrite: true);

                if (alreadyExists)
                {
                    summary.FilesUpdated++;
                    _logger.Info($"Updated file: {sourcePath} -> {replicaPath}");
                }
                else
                {
                    summary.FilesCopied++;
                    _logger.Info($"Copied file: {sourcePath} -> {replicaPath}");
                }
            });
        }
    }

    private void Execute(SyncSummary summary, string decription, Action operation)
    {
        try
        {
            operation();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            summary.Errors++;
            _logger.Error($"Failed to {decription}: {ex.Message}");
        }
    }

    private void ClearReadOnly(string path)
    {
        var file = new FileInfo(path);

        if (file.Exists && file.IsReadOnly)
        {
            file.IsReadOnly = false;
        }
    }

    private static int Depth(string relativePath) =>
        relativePath.Count(character => character == Path.DirectorySeparatorChar);
}
