using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class WeatherApiResponse
{
    [JsonPropertyName("current")]
    public WeatherInfo? Current { get; init; }
    
}