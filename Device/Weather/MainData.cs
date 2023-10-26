using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class MainData
{
    [JsonPropertyName("temp")]
    public double Temperature { get; init; }
}