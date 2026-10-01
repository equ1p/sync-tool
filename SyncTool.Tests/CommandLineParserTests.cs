using SyncTool.Cli;

namespace SyncTool.Tests;

[TestFixture]
public class CommandLineParserTests
{
    private TempFolder _source = null!;
    private TempFolder _replica = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new TempFolder("source");
        _replica = new TempFolder("replica");
    }

    [TearDown]
    public void TearDown()
    {
        _source.Dispose();
        _replica.Dispose();
    }

    private string[] ValidArguments(params string[] extra) =>
    [
        "--source", _source.Root,
        "--replica", _replica.Root,
        "--interval", "30",
        "--log", Path.Combine(Path.GetTempPath(), "synctool-tests", "sync.log"),
        .. extra
    ];

    [Test]
    public void TryParse_WithValidArguments_ReturnsOptions()
    {
        bool parsed = CommandLineParser.TryParse(ValidArguments(), out SyncOptions? options, out string? error);

        Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.True);
                Assert.That(options!.SourcePath, Is.EqualTo(_source.Root));
                Assert.That(options.ReplicaPath, Is.EqualTo(_replica.Root));
                Assert.That(options.Interval, Is.EqualTo(TimeSpan.FromSeconds(30)));
                Assert.That(options.RunOnce, Is.False);
            }
        );
    }

    [Test]
    public void TryParse_WithShortOptionNames_ReturnsOptions()
    {
        string[] args =
        [
            "-s", _source.Root,
            "-r", _replica.Root,
            "-i", "5",
            "-l", Path.Combine(Path.GetTempPath(), "sync.log")
        ];
        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.True, error);
    }

    [Test]
    public void TryParse_WithOnceFlag_SetsRunOnce()
    {
        CommandLineParser.TryParse(ValidArguments("--once"), out SyncOptions? options, out _);

        Assert.That(options!.RunOnce, Is.True);
    }

    [Test]
    public void TryParse_WithRelativePaths_ReturnsAbsolutePaths()
    {
        string[] args =
        [
            "--source", ".",
            "--replica", _replica.Root,
            "--interval", "1",
            "--log", "sync.log"
        ];

        CommandLineParser.TryParse(args, out SyncOptions? options, out _);

        Assert.That(Path.IsPathRooted(options!.SourcePath), Is.True);
        Assert.That(Path.IsPathRooted(options.LogFilePath), Is.True);
    }

    [Test]
    public void TryParse_WithotSource_Fails()
    {
        string [] args =
            [
                "--replica", _replica.Root,
                "--interval", "30",
                "--log", "sync.log"
            ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("--source"));
    }

    [TestCase("0")]
    [TestCase("-5")]
    [TestCase("abc")]
    [TestCase("1.5")]
    public void TryParse_WithInvalidInterval_Fails(string interval)
    {
        string[] args =
            ["--source", _source.Root,
                "--replica", _replica.Root,
                "--interval", interval,
                "--log", "sync.log"
            ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("positive"));
    }

    [Test]
    public void TryParse_WithUnknownOption_Fails()
    {
        Assert.That(CommandLineParser.TryParse(ValidArguments("--verbose"), out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("--verbose"));
    }

    [Test]
    public void TryParse_WithOptionMissingItsValue_Fails()
    {
        string[] args =
            ["--source", _source.Root,
                "--replica", _replica.Root,
                "--interval", "30", "--log"
            ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("requires a value"));
    }

    [Test]
    public void TryParse_WhenSourceDoesNotExist_Fails()
    {
        string missing = Path.Combine(_source.Root, "does-not-exist");
        string[] args =
        [
            "--source", missing,
            "--replica", _replica.Root,
            "--interval", "30",
            "--log", "sync.log"
        ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("does not exist"));
    }

    [Test]
    public void TryParse_WhenReplicaInsideSource_Fails()
    {
        string nested = Path.Combine(_source.Root, "backup");
        string[] args =
        [
            "--source", _source.Root,
            "--replica", nested,
            "--interval", "30",
            "--log", "sync.log"
        ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("inside the source"));
    }

    [Test]
    public void TryParse_WhenLogFileIsInsideReplica_Fails()
    {
        string logInsideReplica = Path.Combine(_replica.Root, "sync.log");
        string[] args =
        [
            "--source", _source.Root,
            "--replica", _replica.Root,
            "--interval", "30",
            "--log", logInsideReplica
        ];

        Assert.That(CommandLineParser.TryParse(args, out _, out string? error), Is.False);
        Assert.That(error, Does.Contain("deleted"));
    }

    [Test]
    public void IsHelpRequested_WithNoArguments_ReturnTrue()
    {
        Assert.That(CommandLineParser.IsHelpRequested([]), Is.True);
        Assert.That(CommandLineParser.IsHelpRequested(["--help"]), Is.True);
        Assert.That(CommandLineParser.IsHelpRequested(["--source", "x"]), Is.False);
    }
}