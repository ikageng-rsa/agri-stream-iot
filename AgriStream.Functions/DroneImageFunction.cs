using AgriStream.Shared;
using AgriStream.Shared.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class DroneImageFunction
{
    private readonly TableClient _auditTable;
    private readonly ILogger<DroneImageFunction> _logger;

    public DroneImageFunction(TableServiceClient tableServiceClient, ILogger<DroneImageFunction> logger)
    {
        _auditTable = tableServiceClient.GetTableClient(StorageNames.ImageAuditTable);
        _auditTable.CreateIfNotExists();
        _logger = logger;
    }

    [Function("LogDroneImageUpload")]
    public async Task Run(
        [BlobTrigger($"{StorageNames.DroneImagesContainer}/{{name}}", Connection = "AzureStorage")] BlobClient blob,
        string name)
    {
        var properties = (await blob.GetPropertiesAsync()).Value;
        properties.Metadata.TryGetValue("fieldId", out var fieldId);
        properties.Metadata.TryGetValue("uploadedBy", out var uploadedBy);

        _logger.LogInformation(
            "Drone image uploaded: {BlobName} ({SizeBytes} bytes, {ContentType}) for field {FieldId} by {UploadedBy}",
            name, properties.ContentLength, properties.ContentType, fieldId, uploadedBy);

        var audit = new ImageAudit
        {
            // RowKey cannot contain '/', which appears in the field-prefixed blob names.
            RowKey = name.Replace('/', '_'),
            BlobName = name,
            ContainerName = blob.BlobContainerName,
            FieldId = fieldId,
            UploadedBy = uploadedBy,
            ContentType = properties.ContentType,
            SizeBytes = properties.ContentLength,
            Status = "Logged",
            ProcessedAt = DateTimeOffset.UtcNow
        };

        await _auditTable.UpsertEntityAsync(audit, TableUpdateMode.Replace);
    }
}
