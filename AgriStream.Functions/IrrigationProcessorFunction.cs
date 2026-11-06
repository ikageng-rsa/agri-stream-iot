using AgriStream.Shared;
using AgriStream.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class IrrigationProcessorFunction
{
    private const string UpdateDeviceSql = """
        UPDATE dbo.DeviceRegistry
        SET OperationalStatus = @Status,
            LastCommand = @LastCommand,
            LastUpdated = SYSUTCDATETIME()
        WHERE DeviceId = @DeviceId;
        """;

    private readonly string _sqlConnectionString;
    private readonly ILogger<IrrigationProcessorFunction> _logger;

    public IrrigationProcessorFunction(IConfiguration configuration, ILogger<IrrigationProcessorFunction> logger)
    {
        _sqlConnectionString = configuration["AzureSql"]
            ?? throw new InvalidOperationException("Connection string 'AzureSql' not found.");
        _logger = logger;
    }

    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger(StorageNames.IrrigationQueue, Connection = "AzureStorage")] IrrigationCommand command)
    {
        var status = command.Action == "Stop" ? "Idle" : "Irrigating";
        var lastCommand = command.Action == "Stop"
            ? $"Stop ({command.Source}, {command.RequestedBy})"
            : $"Start {command.DurationMinutes} min ({command.Source}, {command.RequestedBy})";

        await using var connection = new SqlConnection(_sqlConnectionString);
        await connection.OpenAsync();

        await using var sqlCommand = new SqlCommand(UpdateDeviceSql, connection);
        sqlCommand.Parameters.AddWithValue("@Status", status);
        sqlCommand.Parameters.AddWithValue("@LastCommand", lastCommand);
        sqlCommand.Parameters.AddWithValue("@DeviceId", command.DeviceId);

        var rows = await sqlCommand.ExecuteNonQueryAsync();

        // An unknown device will never succeed on retry, so log it instead of poisoning the queue.
        if (rows == 0)
        {
            _logger.LogWarning("Irrigation command {CommandId} targeted unknown device {DeviceId}", command.CommandId, command.DeviceId);
            return;
        }

        _logger.LogInformation("Device {DeviceId} on {FieldId} set to {Status} by command {CommandId}",
            command.DeviceId, command.FieldId, status, command.CommandId);
    }
}
