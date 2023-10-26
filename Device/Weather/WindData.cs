using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class WindData
{
    [JsonPropertyName("speed")]
    public double Speed { get; init; }
}