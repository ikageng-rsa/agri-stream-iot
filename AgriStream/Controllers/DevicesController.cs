using AgriStream.Data;
using AgriStream.Models;
using AgriStream.Shared;
using AgriStream.Shared.Models;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Controllers;

public class DevicesController : Controller
{
    private readonly AppDbContext _db;
    private readonly TableClient _telemetryTable;

    public DevicesController(AppDbContext db, TableServiceClient tableServiceClient)
    {
        _db = db;
        _telemetryTable = tableServiceClient.GetTableClient(StorageNames.TelemetryTable);
        _telemetryTable.CreateIfNotExists();
    }

    public async Task<IActionResult> Index()
    {
        var devices = await _db.Devices
            .AsNoTracking()
            .OrderBy(d => d.FieldId)
            .ThenBy(d => d.DeviceId)
            .ToListAsync();

        return View(devices);
    }

    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View(new Device());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(Device device)
    {
        if (string.IsNullOrWhiteSpace(device.DeviceId) || string.IsNullOrWhiteSpace(device.FieldId) || string.IsNullOrWhiteSpace(device.DeviceType))
        {
            ModelState.AddModelError(string.Empty, "Device ID, field and type are required.");
            return View(device);
        }

        device.DeviceId = device.DeviceId.Trim().ToUpperInvariant();
        device.FieldId = device.FieldId.Trim().ToUpperInvariant();

        if (await _db.Devices.AnyAsync(d => d.DeviceId == device.DeviceId))
        {
            ModelState.AddModelError(string.Empty, $"Device {device.DeviceId} is already registered.");
            return View(device);
        }

        device.OperationalStatus = "Idle";
        device.LastUpdated = DateTime.UtcNow;

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Generates a day of five-minute readings per soil sensor so dashboards have data to render.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SeedTelemetry()
    {
        var sensors = await _db.Devices
            .AsNoTracking()
            .Where(d => d.DeviceType == "SoilSensor")
            .ToListAsync();

        var random = new Random();
        var now = DateTimeOffset.UtcNow;
        var total = 0;

        foreach (var sensor in sensors)
        {
            var readings = Enumerable.Range(0, 288).Select(i =>
            {
                var reading = SensorReading.Create(sensor.FieldId, sensor.DeviceId, now.AddMinutes(-5 * i));
                reading.SoilMoisture = Math.Round(20 + random.NextDouble() * 25, 1);
                reading.SoilTemperature = Math.Round(14 + random.NextDouble() * 10, 1);
                reading.AirTemperature = Math.Round(16 + random.NextDouble() * 16, 1);
                reading.Humidity = Math.Round(35 + random.NextDouble() * 50, 1);
                reading.BatteryLevel = Math.Round(60 + random.NextDouble() * 40, 0);
                return reading;
            });

            // Table Storage batches must share a partition and hold at most 100 entities.
            foreach (var chunk in readings.Chunk(100))
            {
                await _telemetryTable.SubmitTransactionAsync(
                    chunk.Select(r => new TableTransactionAction(TableTransactionActionType.UpsertReplace, r)));
                total += chunk.Length;
            }
        }

        TempData["Message"] = $"Seeded {total} telemetry readings for {sensors.Count} soil sensors.";
        return RedirectToAction(nameof(Index));
    }
}
