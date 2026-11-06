using AgriStream.Models;
using AgriStream.Shared.Models;

namespace AgriStream.ViewModels;

public class DashboardViewModel
{
    public List<string> Fields { get; set; } = [];
    public string? SelectedField { get; set; }
    public List<SensorReading> Readings { get; set; } = [];
    public List<Device> Valves { get; set; } = [];
    public string? Error { get; set; }

    public const double LowMoistureThreshold = 25;

    public SensorReading? Latest => Readings.FirstOrDefault();

    public bool IsMoistureLow => Latest is not null && Latest.SoilMoisture < LowMoistureThreshold;
}
