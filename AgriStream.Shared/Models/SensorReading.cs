using Azure;
using Azure.Data.Tables;

namespace AgriStream.Shared.Models;

// PartitionKey = FieldId, RowKey = inverted ticks + device so the newest readings sort first.
public class SensorReading : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string FieldId { get; set; } = string.Empty;
    public double SoilMoisture { get; set; }
    public double SoilTemperature { get; set; }
    public double AirTemperature { get; set; }
    public double Humidity { get; set; }
    public double BatteryLevel { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    public static SensorReading Create(string fieldId, string deviceId, DateTimeOffset recordedAt) => new()
    {
        PartitionKey = fieldId,
        RowKey = $"{DateTimeOffset.MaxValue.Ticks - recordedAt.Ticks:D19}_{deviceId}",
        FieldId = fieldId,
        DeviceId = deviceId,
        RecordedAt = recordedAt
    };
}
