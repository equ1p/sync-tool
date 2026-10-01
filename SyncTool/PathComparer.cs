namespace SyncTool;

public static class PathComparer
{
    public static StringComparison Comparison { get;  } =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public static StringComparer Comparer { get; } =
        OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    public static bool AreSame(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), Comparison);


    public static bool IsInside(string candidate, string folder)
    {
        string normalizedFolder = Normalize(folder);
        string normalizedCandidate = Normalize(candidate);

        if (string.Equals(normalizedCandidate, normalizedFolder, Comparison))
        {
            return true;
        }

        string prefix = normalizedFolder + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(prefix, Comparison);
    }

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));


}