using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using PiPanel.Shared.Environment;

namespace PiPanel.Device.Environment;

public class EnvironmentService
{
    public double? LatestTemperature => latestTemperatureInternal;
    public double? LatestHumidity => latestHumidityInternal;

    private readonly TemperatureHumiditySensor sensor;
    private readonly DeviceClient deviceClient;

    private double? latestTemperatureInternal;
    private double? latestHumidityInternal;

    public EnvironmentService(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;

        sensor = new TemperatureHumiditySensor();
    }

    public async void ExecuteAsync(object? state)
    {
        await SendEnvironmentMessageAsync();
    }

    private async Task SendEnvironmentMessageAsync()
    {
        var correlationId = Guid.NewGuid().ToString();

        if (!TryGetEnvironmentStatus(out var environmentStatus))
        {
            Console.WriteLine("Could not get environment status from sensors at {0}", DateTime.Now);
            return;
        }

        Console.WriteLine("Sending environment status: Temperature {0}, Humidity {1}",
            environmentStatus.Temperature, environmentStatus.Humidity);

        var messageContent = JsonSerializer.Serialize(environmentStatus);

        var message = new Message(Encoding.UTF8.GetBytes(messageContent))
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            CorrelationId = correlationId,
        };

        message.Properties.Add("Type", "EnvironmentStatus");

        await deviceClient.SendEventAsync(message);

        Console.WriteLine("Environment status successfully sent ({0})", correlationId);
    }

    private bool TryGetEnvironmentStatus([NotNullWhen(true)] out EnvironmentStatus? environmentStatus)
    {
        environmentStatus = null;

        try
        {
            var temperature = sensor.GetTemperature();
            var humidity = sensor.GetHumidity();

            if (temperature is not null)
            {
                latestTemperatureInternal = temperature;
            }

            if (humidity is not null)
            {
                latestHumidityInternal = humidity;
            }

            if (temperature is null && humidity is null)
            {
                return false;
            }

            environmentStatus = new EnvironmentStatus
            {
                MeasuredAt = DateTime.Now,
                Temperature = temperature,
                Humidity = humidity,
            };

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error retrieving environment status from sensors: {ex.Message}");

            return false;
        }
    }
}