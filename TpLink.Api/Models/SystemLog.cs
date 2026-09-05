namespace TpLink.Api.Models;

/// <summary>One line of the device's system log (admin/syslog?form=log).</summary>
public class SystemLog
{
    /// <summary>Kept as the device's text; it is not a stable timestamp format.</summary>
    public string? Time { get; set; }

    public string? Type { get; set; }
    public string? Level { get; set; }
    public string? Content { get; set; }

    public override string ToString() => $"time: {Time}, type: {Type}, level: {Level}, content: {Content}";
}
