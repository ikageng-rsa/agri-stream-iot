using AgriStream.Shared;
using AgriStream.Shared.Models;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AgriStream.Functions;

public class TelemetryFunction
{
    private const int DefaultTake = 50;
    private const int MaxTake = 500;

    private readonly TableClient _telemetryTable;

    public TelemetryFunction(TableServiceClient tableServiceClient)
    {
        _telemetryTable = tableServiceClient.GetTableClient(StorageNames.TelemetryTable);
        _telemetryTable.CreateIfNotExists();
    }

    [Function("GetLatestTelemetry")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        var fieldId = req.Query["fieldId"].ToString().Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(fieldId))
        {
            return new BadRequestObjectResult("Query string must include fieldId.");
        }

        var take = int.TryParse(req.Query["take"], out var requested) ? Math.Clamp(requested, 1, MaxTake) : DefaultTake;

        // RowKeys use inverted ticks, so a single-partition query already returns newest first.
        var readings = new List<SensorReading>(take);
        await foreach (var reading in _telemetryTable.QueryAsync<SensorReading>(r => r.PartitionKey == fieldId, maxPerPage: take))
        {
            readings.Add(reading);
            if (readings.Count == take)
            {
                break;
            }
        }

        return new OkObjectResult(readings);
    }
}
