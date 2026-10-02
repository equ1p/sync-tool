using System.Security.Cryptography;

namespace SyncTool.Sync;

public static class FileComparer
{
    public static bool HaveSameContent(string leftPath, string rightPath)
    {
        long leftLength = new FileInfo(leftPath).Length;
        long rightLength = new FileInfo(rightPath).Length;

        if (leftLength != rightLength)
        {
            return false;
        }

        return ComputeHash(leftPath).AsSpan().SequenceEqual(ComputeHash(rightPath));
    }

    private static byte[] ComputeHash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return MD5.HashData(stream);
    }
}
