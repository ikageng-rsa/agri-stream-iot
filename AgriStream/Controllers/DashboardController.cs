using AgriStream.Data;
using AgriStream.Models;
using AgriStream.Services;
using AgriStream.Shared.Models;
using AgriStream.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public async Task<IActionResult> TriggerIrrigation(string deviceId, string fieldId, string irrigationAction = "Start", int durationMinutes = 30)
    {
        var command = new IrrigationCommand
        {
            DeviceId = deviceId,
            FieldId = fieldId,
            Action = irrigationAction,
            DurationMinutes = Math.Clamp(durationMinutes, 1, 240),
            Source = "User",
            RequestedBy = User.Identity?.Name
        };

        try
        {
            var response = await _functionsApiClient.TriggerIrrigationAsync(command);
            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = $"{irrigationAction} command for {deviceId} queued. The valve status updates once the command is processed.";
            }
            else
            {
                var detail = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"TriggerIrrigation returned {(int)response.StatusCode}: {detail}";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "TriggerIrrigation failed for device {DeviceId}", deviceId);
            TempData["Error"] = "Could not reach the irrigation service. Please try again.";
        }

        return RedirectToAction(nameof(Index), new { fieldId });
    }
}
