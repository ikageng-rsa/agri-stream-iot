using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var storageConnectionString = builder.Configuration["AzureStorage"]
    ?? throw new InvalidOperationException("Connection string 'AzureStorage' not found.");

builder.Services.AddSingleton(new TableServiceClient(storageConnectionString));
builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
// Queue-triggered functions expect Base64 message bodies by default.
builder.Services.AddSingleton(new QueueServiceClient(storageConnectionString,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Build().Run();
