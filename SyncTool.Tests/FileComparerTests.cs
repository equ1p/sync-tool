using SyncTool.Sync;

namespace SyncTool.Tests;

[TestFixture]
public class FileComparerTests
{
    private TempFolder _folder = null!;

    [SetUp]
    public void SetUp() => _folder = new TempFolder();

    [TearDown]
    public void TearDown() => _folder.Dispose();

    [Test]
    public void HaveSameContent_WithIdenticalContent_ReturnsTrue()
    {
        string left = _folder.WriteFile("left.txt", "same content");
        string right = _folder.WriteFile("right.txt", "same content");

        Assert.That(FileComparer.HaveSameContent(left, right), Is.True);
    }

    [Test]
    public void HasSameContent_WithDiffernetContent_ReturnsFalse()
    {
        string left = _folder.WriteFile("left.txt", "short");
        string right = _folder.WriteFile("righ.txt", "much longer");

        Assert.That(FileComparer.HaveSameContent(left, right), Is.False);
    }

    [Test]
    public void HaveSameContent_WithSameLengthButDifferentContent_ReturnsFalse()
    {
        string left = _folder.WriteFile("left.txt", "AAAA");
        string right = _folder.WriteFile("right.txt", "BBBB");

        Assert.That(FileComparer.HaveSameContent(left, right), Is.False);
    }

    [Test]
    public void HaveSameContent_WithEmptyFiles_ReturnsTrue()
    {
        string left = _folder.WriteFile("left.txt", string.Empty);
        string right = _folder.WriteFile("right.txt", string.Empty);

        Assert.That(FileComparer.HaveSameContent(left, right), Is.True);
    }
}
