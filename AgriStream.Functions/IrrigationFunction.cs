using System.Text.Json;
using AgriStream.Shared;
using AgriStream.Shared.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AgriStream.Functions;

public class IrrigationFunction
{
    private static readonly string[] ValidActions = ["Start", "Stop"];

    private readonly QueueClient _queue;

    public IrrigationFunction(QueueServiceClient queueServiceClient)
    {
        _queue = queueServiceClient.GetQueueClient(StorageNames.IrrigationQueue);
        _queue.CreateIfNotExists();
    }

    [Function("TriggerIrrigation")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var command = await req.ReadFromJsonAsync<IrrigationCommand>();

        if (command is null || string.IsNullOrWhiteSpace(command.DeviceId) || string.IsNullOrWhiteSpace(command.FieldId))
        {
            return new BadRequestObjectResult("Request body must include DeviceId and FieldId.");
        }

        if (!ValidActions.Contains(command.Action))
        {
            return new BadRequestObjectResult("Action must be Start or Stop.");
        }

        command.DeviceId = command.DeviceId.Trim().ToUpperInvariant();
        command.FieldId = command.FieldId.Trim().ToUpperInvariant();
        command.RequestedAt = DateTimeOffset.UtcNow;

        var receipt = await _queue.SendMessageAsync(JsonSerializer.Serialize(command));

        return new AcceptedResult(location: (string?)null, value: new { command.CommandId, messageId = receipt.Value.MessageId });
    }
}
