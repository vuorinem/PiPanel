namespace PiPanel.Shared.Environment;

public class EnvironmentStatus
{
    public DateTime MeasuredAt { get; set; }

    public double? Temperature { get; set; }

    public double? Humidity { get; set; }
}