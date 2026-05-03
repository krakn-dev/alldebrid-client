using System.ComponentModel;

namespace AdbClient.Data.Enums;

public enum AuthenticationType
{
    [Description("Username + Password")]
    UserNamePassword,

    [Description("No Authentication")]
    None
}