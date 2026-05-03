using System.ComponentModel;

namespace AdbClient.Data.Enums;

public enum DownloadClientLogLevel
{
    [Description("Verbose")]
    Verbose,

    [Description("Debug")]
    Debug,

    [Description("Information")]
    Information,

    [Description("Warning")]
    Warning,

    [Description("Error")]
    Error,

    [Description("None")]
    None
}