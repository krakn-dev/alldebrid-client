namespace RdtClient.Data.Models.Internal;

public class AppSettings
{
    public string DataPath { get; set; } = "./data";
    public AppSettingsLogging? Logging { get; set; }
    public AppSettingsDatabase? Database { get; set; }

    public int Port { get; set; }
    public string? BasePath { get; set; }
}

public class AppSettingsLogging
{
    public AppSettingsLoggingFile? File { get; set; }
}
    
public class AppSettingsLoggingFile
{
    public string? Path { get; set; }
    public long FileSizeLimitBytes { get; set; }
    public int MaxRollingFiles { get; set; }
}

public class AppSettingsDatabase
{
    public string? Path { get; set; }
}