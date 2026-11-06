using AgriStream.Data;
using AgriStream.Services;
using AgriStream.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly FunctionsApiClient _functionsApiClient;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext db, FunctionsApiClient functionsApiClient, ILogger<DashboardController> logger)
    {
        _db = db;
        _functionsApiClient = functionsApiClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? fieldId)
    {
        var model = new DashboardViewModel
        {
            Fields = await _db.Devices
                .AsNoTracking()
                .Select(d => d.FieldId)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync()
        };

        model.SelectedField = model.Fields.Contains(fieldId ?? string.Empty) ? fieldId : model.Fields.FirstOrDefault();
        if (model.SelectedField is null)
        {
            return View(model);
        }

        model.Valves = await _db.Devices
            .AsNoTracking()
            .Where(d => d.FieldId == model.SelectedField && d.DeviceType == "IrrigationValve")
            .OrderBy(d => d.DeviceId)
            .ToListAsync();

        try
        {
            model.Readings = await _functionsApiClient.GetLatestTelemetryAsync(model.SelectedField);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "GetLatestTelemetry failed for field {FieldId}", model.SelectedField);
            model.Error = "Telemetry is temporarily unavailable. Please try again shortly.";
        }

        return View(model);
    }
}
