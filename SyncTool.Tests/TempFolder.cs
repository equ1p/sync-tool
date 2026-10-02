namespace SyncTool.Tests;

public sealed class TempFolder : IDisposable
{
    private readonly string _scope;

    public TempFolder(string name = "folder")
    {
        _scope = Path.Combine(Path.GetTempPath(), "synctool-tests", Guid.NewGuid().ToString("N"));
        Root = Path.Combine(_scope, name);
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Combine(string relativePath) => Path.Combine(Root, relativePath);

    public string WriteFile(string relativePath, string content)
    {
        string fullPath = Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public string ReadFile(string relativePath) => File.ReadAllText(Combine(relativePath));
    public bool FileExists(string relativePath) => File.Exists(Combine(relativePath));
    public bool DirectoryExists(string relativePath) => Directory.Exists(Combine(relativePath));

    public IReadOnlyCollection<string> AllFiles() =>
        Directory.Exists(Root)
            ? Directory.GetFiles(Root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(Root, path))
                .ToArray()
            : [];

    public void Dispose()
    {
        try
        {
            Directory.Delete(_scope, recursive: true);
        }
        catch (IOException)
        {

        }
    }



}