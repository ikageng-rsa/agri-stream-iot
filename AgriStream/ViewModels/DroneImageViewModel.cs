namespace AgriStream.ViewModels;

public class DroneImageViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? FieldId { get; set; }
    public string? UploadedBy { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string AuditStatus { get; set; } = "Pending";
    public DateTimeOffset? AuditedAt { get; set; }
}
