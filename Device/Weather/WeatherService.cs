using System.Net.Http.Json;
using Microsoft.WindowsAzure.Storage.Core;
using PiPanel.Shared;

namespace PiPanel.Device.Weather;

public class WeatherService
{
    private static readonly Uri CurrentWeatherUri = new("https://api.openweathermap.org/data/2.5/weather");

    private static readonly Uri ForecastUri = new("https://api.openweathermap.org/data/2.5/forecast");

    private readonly HttpClient http;

    private readonly DeviceProperties deviceProperties;

    private readonly string apiKey;

    public WeatherService(DeviceProperties deviceProperties, string apiKey)
    {
        this.deviceProperties = deviceProperties;
        this.apiKey = apiKey;

        http = new HttpClient();
    }

    public async Task<WeatherInfo?> GetCurrentWeather()
    {
        if (deviceProperties.Location is null)
        {
            return null;
        }

        var currentUri = BuildCurrentWeatherUri(deviceProperties.Location, apiKey);
        var forecastUri = BuildForecastUri(deviceProperties.Location, apiKey);

        var current = await http.GetFromJsonAsync<WeatherData>(currentUri);
        var forecast = await http.GetFromJsonAsync<ForecastResponse>(forecastUri);

        return new WeatherInfo
        {
            Temperature = current?.Main.Temperature,
            WindSpeed = current?.Wind.Speed,
            RainProbability = forecast?.List.FirstOrDefault()?.ProbabilityOfPrecipitation,
        };
    }

    private static Uri BuildCurrentWeatherUri(Location location, string apiKey)
    {
        var uriBuilder = new UriBuilder(CurrentWeatherUri);

        var queryBuilder = new UriQueryBuilder();

        queryBuilder.Add("lat", location.Latitude.ToString());
        queryBuilder.Add("lon", location.Longitude.ToString());
        queryBuilder.Add("units", "metric");
        queryBuilder.Add("appid", apiKey);

        return queryBuilder.AddToUri(uriBuilder.Uri);
    }

    private static Uri BuildForecastUri(Location location, string apiKey)
    {
        var uriBuilder = new UriBuilder(ForecastUri);

        var queryBuilder = new UriQueryBuilder();

        queryBuilder.Add("lat", location.Latitude.ToString());
        queryBuilder.Add("lon", location.Longitude.ToString());
        queryBuilder.Add("units", "metric");
        queryBuilder.Add("cnt", "1");
        queryBuilder.Add("appid", apiKey);

        return queryBuilder.AddToUri(uriBuilder.Uri);
    }
}