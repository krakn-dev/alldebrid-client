namespace RdtClient.Data.Models.Internal;

public class SettingProperty
{
    public string Key { get; set; } = default!;
    public Object? Value { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public Dictionary<int, string>? EnumValues { get; set; }
}