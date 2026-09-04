using System.IO.Abstractions;

namespace AdbClient.Service.Helpers;

internal static class FileSystemPath
{
    public static StringComparer Comparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static string Normalize(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    public static bool IsSameOrDescendant(string path, string root)
    {
        return PathsEqual(path, root) || IsStrictDescendant(path, root);
    }

    public static bool IsStrictDescendant(string path, string root)
    {
        var normalizedPath = Normalize(path);
        var normalizedRoot = Normalize(root);
        var rootPrefix = Path.EndsInDirectorySeparator(normalizedRoot)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(rootPrefix, PathComparison);
    }

    public static bool PathsEqual(string left, string right)
    {
        return string.Equals(Normalize(left), Normalize(right), PathComparison);
    }

    public static bool ContainsReparsePoint(IFileSystem fileSystem, string path, string boundary)
    {
        var current = Normalize(path);
        var normalizedBoundary = Normalize(boundary);

        if (!IsSameOrDescendant(current, normalizedBoundary))
        {
            return true;
        }

        while (IsSameOrDescendant(current, normalizedBoundary))
        {
            if (fileSystem.Directory.Exists(current) &&
                fileSystem.File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }

            if (PathsEqual(current, normalizedBoundary))
            {
                break;
            }

            var parent = fileSystem.Path.GetDirectoryName(current);

            if (string.IsNullOrWhiteSpace(parent) || PathsEqual(parent, current))
            {
                break;
            }

            current = Normalize(parent);
        }

        return false;
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
