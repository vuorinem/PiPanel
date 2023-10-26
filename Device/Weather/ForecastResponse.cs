using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class ForecastResponse
{
    [JsonPropertyName("list")]
    public IList<WeatherData> List { get; init; } = default!;
    
}