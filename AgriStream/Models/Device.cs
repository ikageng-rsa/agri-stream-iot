namespace AgriStream.Models;

public class Device
{
    public string DeviceId { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string OperationalStatus { get; set; } = "Idle";
    public string? LastCommand { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
