using System.Net.Http.Json;
using AgriStream.Shared.Models;

namespace AgriStream.Services;

public class FunctionsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _functionKey;

    public FunctionsApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _functionKey = configuration["Functions:Key"]?.Trim();
    }

    public async Task<List<SensorReading>> GetLatestTelemetryAsync(string fieldId, int take = 50)
    {
        var url = WithKey($"GetLatestTelemetry?fieldId={Uri.EscapeDataString(fieldId)}&take={take}");
        return await _httpClient.GetFromJsonAsync<List<SensorReading>>(url) ?? [];
    }

    public Task<HttpResponseMessage> TriggerIrrigationAsync(IrrigationCommand command) =>
        _httpClient.PostAsJsonAsync(WithKey("TriggerIrrigation"), command);

    private string WithKey(string pathAndQuery)
    {
        if (string.IsNullOrEmpty(_functionKey))
        {
            return pathAndQuery;
        }

        var separator = pathAndQuery.Contains('?') ? '&' : '?';
        return $"{pathAndQuery}{separator}code={Uri.EscapeDataString(_functionKey)}";
    }
}
