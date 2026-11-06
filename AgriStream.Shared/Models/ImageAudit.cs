using Azure;
using Azure.Data.Tables;

namespace AgriStream.Shared.Models;

// PartitionKey = "DroneImage", RowKey = blob name.
public class ImageAudit : ITableEntity
{
    public string PartitionKey { get; set; } = "DroneImage";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string BlobName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string? FieldId { get; set; }
    public string? UploadedBy { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string Status { get; set; } = "Received";
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
