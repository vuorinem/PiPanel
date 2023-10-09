using Iot.Device.DHTxx;

namespace PiPanel.Device.Environment;

public class TemperatureHumiditySensor
{
    private const int GpioPinNumber = 22;
    private Dht11 sensor;

    public TemperatureHumiditySensor()
    {
        sensor = new Dht11(GpioPinNumber);
    }

    public double? GetTemperature()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (sensor.TryReadTemperature(out var temperature))
            {
                return temperature.DegreesCelsius;
            }

            Thread.Sleep(100);
        }

        Console.Error.WriteLine($"Cannot read temperature sensor");

        return null;
    }

    public double? GetHumidity()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (sensor.TryReadHumidity(out var humidity))
            {
                return humidity.Percent;
            }

            Thread.Sleep(100);
        }

        Console.Error.WriteLine($"Cannot read humidity sensor");

        return null;
    }
}