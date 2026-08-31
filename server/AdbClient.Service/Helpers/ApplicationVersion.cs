using System.Reflection;

namespace AdbClient.Service.Helpers;

public static class ApplicationVersion
{
    public static Version? Current => Assembly.GetEntryAssembly()?.GetName().Version;

    public static string? CurrentText => Format(Current);

    public static string? CurrentTag => FormatTag(Current);

    public static string? Format(Version? version)
    {
        if (version == null)
        {
            return null;
        }

        return $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    }

    public static string? FormatTag(Version? version)
    {
        var formatted = Format(version);
        return formatted == null ? null : $"v{formatted}";
    }

    public static bool IsNewerRelease(string? releaseTag, string? currentTag)
    {
        return TryParseTag(releaseTag, out var releaseVersion) &&
               TryParseTag(currentTag, out var currentVersion) &&
               releaseVersion > currentVersion;
    }

    private static bool TryParseTag(string? tag, out Version version)
    {
        var value = tag?.Trim().TrimStart('v', 'V');
        return Version.TryParse(value, out version!);
    }
}
