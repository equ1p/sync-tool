using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SyncTool.Cli;

public static class CommandLineParser
{
    public const string Usage = """
                                SyncTool - one-way folder synchronization.

                                Usage:
                                  SyncTool --source <path> --replica <path> --interval <seconds> --log <file> [--once]

                                Options:
                                  -s, --source   <path>     Folder to read from. Must exist. Never modified.
                                  -r, --replica  <path>     Folder kept identical to the source. Created if missing.
                                  -i, --interval <seconds>  Delay between runs. Positive whole number of seconds.
                                  -l, --log      <file>     Log file. Appended to; parent folders are created.
                                      --once                Synchronize once and exit instead of looping.
                                  -h, --help                Show this help.

                                Example:
                                  SyncTool --source ./data --replica ./backup --interval 30 --log ./sync.log
                                """;

    public static bool IsHelpRequested(string[] args) =>
        args.Length == 0 || args.Any(argument => argument is "-h" or "--help");

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out SyncOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        options = null;
        string? source = null;
        string? replica = null;
        string? interval = null;
        string? log = null;
        bool runOnce = false;

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];

            switch (argument)
            {
                case "-s" or "--source":
                    if(!TryTakeValue(args, ref i, argument, out source, out error)) return false;
                    break;
                case "-r" or "--replica":
                    if(!TryTakeValue(args, ref i, argument, out replica, out error)) return false;
                    break;
                case "-i" or "--interval":
                    if(!TryTakeValue(args, ref i, argument, out interval, out error)) return false;
                    break;
                case "-l" or "--log":
                    if(!TryTakeValue(args, ref i, argument, out log, out error)) return false;
                    break;
                case "--once":
                    runOnce = true;
                    break;
                default:
                    error = $"Unknown argument: {argument}";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(source)) { error = "Missing required option '--source'."; return false; }
        if (string.IsNullOrWhiteSpace(replica)) { error = "Missing required option '--replica'."; return false; }
        if (string.IsNullOrWhiteSpace(interval)) { error = "Missing required option '--interval'."; return false; }
        if (string.IsNullOrWhiteSpace(log)) { error = "Missing required option '--log'."; return false; }

        if (!int.TryParse(interval, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            || seconds <= 0)
        {
            error = $"Interval must be a positive whole number of seconds, but was '{interval}'.";
            return false;
        }

        string sourceFullPath;
        string replicaFullPath;
        string logFullPath;

        try
        {
            sourceFullPath = Path.GetFullPath(source);
            replicaFullPath = Path.GetFullPath(replica);
            logFullPath = Path.GetFullPath(log);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"Invalid path: {ex.Message}";
            return false;
        }

        if (!Directory.Exists(sourceFullPath))
        {
            error = $"Source folder '{sourceFullPath}' does not exist.";
            return false;
        }

        if (PathComparer.AreSame(sourceFullPath, replicaFullPath))
        {
            error = "Source and replica folders must be two different folders.";
            return false;
        }

        if (PathComparer.IsInside(replicaFullPath, sourceFullPath))
        {
            error = "The replica folder must not be inside the source folder.";
            return false;
        }

        if (PathComparer.IsInside(sourceFullPath, replicaFullPath))
        {
            error = "The source folder must not be inside the replica folder.";
            return false;
        }

        if (PathComparer.IsInside(logFullPath, replicaFullPath))
        {
            error = "The log file must not be inside the replica folder: it would be deleted on the next run.";
            return false;
        }

        options = new SyncOptions(sourceFullPath, replicaFullPath,
            TimeSpan.FromSeconds(seconds), logFullPath, runOnce);
        error = null;
        return true;
    }

    private static bool TryTakeValue(
        string[] args,
        ref int index,
        string optionName,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out string? error)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            error = $"Option '{optionName}' requires a value.";
            return false;
        }

        value = args[++index];
        error = null;
        return true;
    }
}