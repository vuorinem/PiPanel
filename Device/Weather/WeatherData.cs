using System.Text.Json.Serialization;

namespace PiPanel.Device.Weather;

public class WeatherData
{
    [JsonPropertyName("main")]
    public MainData Main { get; init; } = default!;
    
    [JsonPropertyName("wind")]
    public WindData Wind { get; init; } = default!;
    
    [JsonPropertyName("pop")]
    public double ProbabilityOfPrecipitation { get; init; } = default!;
}