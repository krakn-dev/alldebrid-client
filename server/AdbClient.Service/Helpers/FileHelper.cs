using System.Text;

namespace AdbClient.Service.Helpers;

public static class FileHelper
{
    public static async Task Delete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            return;
        }

        var retry = 0;

        while (true)
        {
            try
            {
                File.Delete(path);

                break;
            }
            catch
            {
                if (retry >= 3)
                {
                    throw;
                }

                retry++;

                await Task.Delay(1000 * retry);
            }
        }
    }

    public static async Task DeleteDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            return;
        }

        var retry = 0;

        while (true)
        {
            try
            {
                Directory.Delete(path, true);

                break;
            }
            catch
            {
                if (retry >= 3)
                {
                    throw;
                }

                retry++;

                await Task.Delay(1000 * retry);
            }
        }
    }

    public static string RemoveInvalidFileNameChars(string filename)
    {
        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }
    
    public static string GetDirectoryContents(string path)
    {
        var stringBuilder = new StringBuilder();
        GetDirectoryContents(path, stringBuilder, "");
        return stringBuilder.ToString();
    }

    private static void GetDirectoryContents(string path, StringBuilder stringBuilder, string indent)
    {
        var directoryInfo = new DirectoryInfo(path);

        var directories = directoryInfo.GetDirectories();
        foreach (var directory in directories)
        {
            stringBuilder.AppendLine($"{indent}{directory.Name}");
            GetDirectoryContents(directory.FullName, stringBuilder, indent + "  ");
        }

        var files = directoryInfo.GetFiles();
        foreach (var file in files)
        {
            stringBuilder.AppendLine($"{indent}{file.Name}");
        }
    }
}