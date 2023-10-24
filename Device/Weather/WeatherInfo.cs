using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class WeatherInfo
{
    [JsonPropertyName("time")]
    public DateTime MeasuredAt { get; init; }

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; init; }

    [JsonPropertyName("rain")]
    public double Rain { get; init; }

    [JsonPropertyName("windspeed_10m")]
    public double WindSpeed { get; init; }
}