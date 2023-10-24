using System.Net.Http.Json;
using Microsoft.WindowsAzure.Storage.Core;
using PiPanel.Shared;

namespace PiPanel.Device.Weather;

public class WeatherService
{
    private readonly Uri ForecastUri = new("https://api.open-meteo.com/v1/forecast");

    private readonly HttpClient http;

    private readonly DeviceProperties deviceProperties;

    public WeatherService(DeviceProperties deviceProperties)
    {
        this.deviceProperties = deviceProperties;

        http = new HttpClient();
    }

    public async Task<WeatherInfo?> GetCurrentWeather()
    {
        if (deviceProperties.Location is null)
        {
            return null;
        }

        var uri = BuildForecastUri(deviceProperties.Location);
        var response = await http.GetFromJsonAsync<WeatherApiResponse>(uri);

        if (response is null)
        {
            Console.Error.WriteLine("Error retrieving weather from {0}", uri);

            return null;
        }

        return response.Current;
    }

    private Uri BuildForecastUri(Location location)
    {
        var uriBuilder = new UriBuilder(ForecastUri);

        var queryBuilder = new UriQueryBuilder();

        queryBuilder.Add("latitude", location.Latitude.ToString());
        queryBuilder.Add("longitude", location.Longitude.ToString());
        queryBuilder.Add("current", "temperature_2m,rain,windspeed_10m");
        queryBuilder.Add("timezone", "Europe/London");

        return queryBuilder.AddToUri(uriBuilder.Uri);
    }
}