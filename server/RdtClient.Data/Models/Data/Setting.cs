using System.ComponentModel.DataAnnotations;

namespace RdtClient.Data.Models.Data;

public class Setting
{
    [Key]
    public string SettingId { get; set; } = null!;

    public string? Value { get; set; }
}