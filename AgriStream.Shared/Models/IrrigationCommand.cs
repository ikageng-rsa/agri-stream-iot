namespace AgriStream.Shared.Models;

public class IrrigationCommand
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public string Action { get; set; } = "Start";
    public int DurationMinutes { get; set; } = 30;
    public string Source { get; set; } = "User";
    public string? RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}
