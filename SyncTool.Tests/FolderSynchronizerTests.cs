using SyncTool.Logging;
using SyncTool.Sync;

namespace SyncTool.Tests;

[TestFixture]
public sealed class FolderSynchronizerTests
{
    private TempFolder _source = null!;
    private TempFolder _replica = null!;
    private RecordingLogger _logger = null!;
    private FolderSynchronizer _synchronizer = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new TempFolder("source");
        _replica = new TempFolder("replica");
        _logger = new RecordingLogger();
        _synchronizer = new FolderSynchronizer(_source.Root, _replica.Root, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _source.Dispose();
        _replica.Dispose();
    }

    private void AssertReplicaMatchesSource()
    {
        Assert.That(_replica.AllFiles(), Is.EquivalentTo(_source.AllFiles()));

        foreach (string relativePath in _source.AllFiles())
        {
            Assert.That(
                _replica.ReadFile(relativePath),
                Is.EqualTo(_source.ReadFile(relativePath)),
                $"content of '{relativePath}' differs");
        }
    }

    [Test]
    public void Synchronize_CopiesFilesAndNestedDirectories()
    {
        _source.WriteFile("root.txt", "root");
        _source.WriteFile(Path.Combine("docs", "guide.txt"), "guide");
        _source.WriteFile(Path.Combine("docs", "images", "logo.bin"), "binary-ish");

        SyncSummary summary = _synchronizer.Synchronize();

        AssertReplicaMatchesSource();
        Assert.That(summary.FilesCopied, Is.EqualTo(3));
        Assert.That(summary.DirectoriesCreated, Is.EqualTo(2));
    }

    [Test]
    public void Synchronize_CreatesReplicaRootWhenItDoesNotExist()
    {
        string missingReplica = Path.Combine(_replica.Root, "nested", "target");
        var synchronizer = new FolderSynchronizer(_source.Root, missingReplica, _logger);
        _source.WriteFile("file.txt", "content");

        synchronizer.Synchronize();

        Assert.That(File.Exists(Path.Combine(missingReplica, "file.txt")), Is.True);
    }

    [Test]
    public void Synchronize_UpdatesFileWhoseContentChangedWithoutChangingLength()
    {
        _source.WriteFile("data.txt", "AAAA");
        _synchronizer.Synchronize();

        _source.WriteFile("data.txt", "BBBB");
        SyncSummary summary = _synchronizer.Synchronize();

        Assert.That(_replica.ReadFile("data.txt"), Is.EqualTo("BBBB"));
        Assert.That(summary.FilesUpdated, Is.EqualTo(1));
        Assert.That(summary.FilesCopied, Is.Zero);
    }

    [Test]
    public void Synchronize_DeletesFileThatIsNoLongerInSource()
    {
        _source.WriteFile("keep.txt", "keep");
        _replica.WriteFile("stale.txt", "stale");

        SyncSummary summary = _synchronizer.Synchronize();

        Assert.That(_replica.FileExists("stale.txt"), Is.False);
        Assert.That(summary.FilesDeleted, Is.EqualTo(1));
        AssertReplicaMatchesSource();
    }

    [Test]
    public void Synchronize_DeletesDirectoryTreeThatIsNoLongerInSource()
    {
        _source.WriteFile("keep.txt", "keep");
        _replica.WriteFile(Path.Combine("stale", "deep", "file.txt"), "stale");

        SyncSummary summary = _synchronizer.Synchronize();

        Assert.That(_replica.DirectoryExists("stale"), Is.False);
        Assert.That(summary.DirectoriesDeleted, Is.EqualTo(2));
        Assert.That(summary.FilesDeleted, Is.EqualTo(1));
    }

    [Test]
    public void Synchronize_ReplacesReplicaFileWithDirectoryOfTheSameName()
    {
        _source.WriteFile(Path.Combine("notes", "today.txt"), "note");
        _replica.WriteFile("notes", "this used to be a file");

        _synchronizer.Synchronize();

        Assert.That(_replica.DirectoryExists("notes"), Is.True);
        AssertReplicaMatchesSource();
    }

    [Test]
    public void Synchronize_ReplacesReplicaDirectoryWithFileOfTheSameName()
    {
        _source.WriteFile("notes", "this is a file now");
        _replica.WriteFile(Path.Combine("notes", "old.txt"), "old");

        _synchronizer.Synchronize();

        Assert.That(_replica.FileExists("notes"), Is.True);
        AssertReplicaMatchesSource();
    }

    [Test]
    public void Synchronize_RunTwice_ReportsNoChangesTheSecondTime()
    {
        _source.WriteFile("a.txt", "a");
        _source.WriteFile(Path.Combine("sub", "b.txt"), "b");
        _synchronizer.Synchronize();

        SyncSummary second = _synchronizer.Synchronize();

        Assert.That(second.HasChanges, Is.False);
        Assert.That(second.Errors, Is.Zero);
    }

    [Test]
    public void Synchronize_NeverModifiesSource()
    {
        _source.WriteFile("a.txt", "a");
        _replica.WriteFile("intruder.txt", "should be deleted from the replica only");
        IReadOnlyCollection<string> before = _source.AllFiles();

        _synchronizer.Synchronize();

        Assert.That(_source.AllFiles(), Is.EquivalentTo(before));
        Assert.That(_source.ReadFile("a.txt"), Is.EqualTo("a"));
    }

    [Test]
    public void Synchronize_LogsEveryOperation()
    {
        _source.WriteFile(Path.Combine("sub", "new.txt"), "new");
        _replica.WriteFile("stale.txt", "stale");

        _synchronizer.Synchronize();

        Assert.Multiple(() =>
        {
            Assert.That(_logger.Messages, Has.Some.Contains("Created directory"));
            Assert.That(_logger.Messages, Has.Some.Contains("Copied file"));
            Assert.That(_logger.Messages, Has.Some.Contains("Deleted file"));
            Assert.That(_logger.Entries, Has.None.Matches<(LogLevel Level, string Message)>(
                entry => entry.Level == LogLevel.Error));
        });
    }

    [Test]
    public void Synchronize_WithAlreadyCancelledToken_Throws()
    {
        _source.WriteFile("a.txt", "a");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => _synchronizer.Synchronize(cts.Token));
    }
}
