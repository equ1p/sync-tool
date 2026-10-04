using SyncTool.Cli;
using SyncTool.Logging;
using SyncTool.Sync;

namespace SyncTool;

public static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitInvalidArguments = 1;
    private const int ExitFailure = 2;

    public static async Task<int> Main(string[] args)
    {
        if (CommandLineParser.IsHelpRequested(args))
        {
            Console.WriteLine(CommandLineParser.Usage);
            return ExitSuccess;
        }

        if (!CommandLineParser.TryParse(args, out SyncOptions? options, out string? error))
        {
            Console.Error.WriteLine($"Error: {error}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(CommandLineParser.Usage);
            return ExitInvalidArguments;
        }

        try
        {
            using var sinks = new CompositeLogger(new ConsoleLogger(), new FileLogger(options.LogFilePath));
            return await RunAsync(options, sinks);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Error: cannot write to log file '{options.LogFilePath}': {ex.Message}");
            return ExitFailure;
        }
    }

    private static async Task<int> RunAsync(SyncOptions options, ILogger logger)
    {
        logger.Info($"Source:   {options.SourcePath}");
        logger.Info($"Replica:  {options.ReplicaPath}");
        logger.Info($"Log file: {options.LogFilePath}");
        logger.Info(options.RunOnce
            ? "Mode: single run."
            : $"Mode: every {options.Interval.TotalSeconds:0} second(s). Press Ctrl+C to stop.");

        var synchronizer = new FolderSynchronizer(options.SourcePath, options.ReplicaPath, logger);
        using CancellationTokenSource shutdown = CreateShutdownTokenSource(logger);

        try
        {
            if (options.RunOnce)
            {
                RunOnce(synchronizer, logger, shutdown.Token);
            }
            else
            {
                await RunPeriodicallyAsync(synchronizer, options.Interval, logger, shutdown.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // The expected way out: Ctrl+C was pressed.
        }

        logger.Info("Stopped.");
        return ExitSuccess;
    }

    private static async Task RunPeriodicallyAsync(
        FolderSynchronizer synchronizer,
        TimeSpan interval,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);

        do
        {
            RunOnce(synchronizer, logger, cancellationToken);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private static void RunOnce(FolderSynchronizer synchronizer, ILogger logger, CancellationToken cancellationToken)
    {
        logger.Info("Synchronization started.");

        try
        {
            SyncSummary summary = synchronizer.Synchronize(cancellationToken);

            logger.Info(summary.HasChanges
                ? $"Synchronization finished: {summary}"
                : "Synchronization finished: the replica was already up to date.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error($"Synchronization run failed: {ex.Message}");
        }
    }

    private static CancellationTokenSource CreateShutdownTokenSource(ILogger logger)
    {
        var shutdown = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            logger.Info("Shutdown requested, stopping after the current operation.");
            shutdown.Cancel();
        };

        return shutdown;
    }
}