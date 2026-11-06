using AgriStream.Data;
using AgriStream.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Controllers;

public class DevicesController : Controller
{
    private readonly AppDbContext _db;

    public DevicesController(AppDbContext db)
    {
        _db = db;
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
}
