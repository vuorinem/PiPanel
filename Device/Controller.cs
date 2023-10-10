using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using PiPanel.Device.Camera;
using PiPanel.Device.Environment;
using PiPanel.Shared;
using PiPanel.Shared.Camera;

namespace PiPanel.Device;

public class Controller
{
    private readonly DeviceClient deviceClient;

    private readonly DeviceProperties deviceProperties;

    private readonly TimeSpan controllerSleepInterval = TimeSpan.FromMinutes(1);

    private readonly TimeSpan DefaultCameraInterval = TimeSpan.FromMinutes(20);

    private readonly TimeSpan DefaultEnvironmentInterval = TimeSpan.FromMinutes(5);

    private bool isRunning = false;

    private CancellationTokenSource? controllerSleepTokenSource;

    private Timer? cameraTimer;

    private Timer? environmentTimer;

    public Controller(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;

        // Initialize with default properties
        deviceProperties = new DeviceProperties
        {
            Cameras = CameraList.Cameras,
            CameraInterval = DefaultCameraInterval,
            EnvironmentInterval = DefaultEnvironmentInterval,
        };
    }

    public async Task RunAsync()
    {
        isRunning = true;

        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);
        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null);

        await ReportCurrentPropertiesAsync();

        var cameraService = new CameraService(deviceClient);
        cameraTimer = new Timer(cameraService.ExecuteAsync, null, TimeSpan.Zero, deviceProperties.CameraInterval);

        var environmentService = new EnvironmentService(deviceClient);
        environmentTimer = new Timer(environmentService.ExecuteAsync, null, TimeSpan.Zero, deviceProperties.EnvironmentInterval);

        Console.WriteLine("Starting controller");

        while (isRunning)
        {
            try
            {
                using (controllerSleepTokenSource = new CancellationTokenSource())
                {
                    Console.WriteLine($"Controller is running, sleeping for {controllerSleepInterval}");
                    await Task.Delay(controllerSleepInterval, controllerSleepTokenSource.Token);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            finally
            {
                controllerSleepTokenSource = null;
            }
        }

        Console.WriteLine("Stopping controller timers");

        await cameraTimer.DisposeAsync();
        await environmentTimer.DisposeAsync();

        Console.WriteLine("Controller stopped");
    }

    public void Stop()
    {
        isRunning = false;
        controllerSleepTokenSource?.Cancel();
    }

    private async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
    {
        Console.WriteLine("Received property changed notification");

        Console.WriteLine(JsonSerializer.Serialize(desiredProperties));

        await UpdateDeviceProperties(desiredProperties);
    }

    private async Task OnReceiveMessage(Message message, object userContext)
    {
        using var bodyReader = new StreamReader(message.BodyStream);
        var body = await bodyReader.ReadToEndAsync();

        Console.WriteLine($"Received message: {body}");

        await deviceClient.CompleteAsync(message);
    }

    private async void OnConnectionStatusChanges(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        Console.WriteLine($"Connection status: {status} ({reason})");

        if (status == ConnectionStatus.Connected)
        {
            var deviceTwin = await deviceClient.GetTwinAsync();

            await UpdateDeviceProperties(deviceTwin.Properties.Desired);
        }
    }

    private async Task UpdateDeviceProperties(TwinCollection desiredProperties)
    {
        var reportedProperties = new TwinCollection();

        foreach (KeyValuePair<string, object> property in desiredProperties)
        {
            switch (property.Key)
            {
                case nameof(DeviceProperties.CameraInterval):
                    if (TryGetValueFromProperty<TimeSpan>(property.Value, out var desiredCameraInterval))
                    {
                        deviceProperties.CameraInterval = desiredCameraInterval;
                        cameraTimer?.Change(TimeSpan.Zero, deviceProperties.CameraInterval);
                        reportedProperties[nameof(DeviceProperties.CameraInterval)] = property.Value;

                        Console.WriteLine("Camera interval set to {0}", deviceProperties.CameraInterval);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse camera interval from value {0}", property.Value);
                    }
                    break;

                case nameof(DeviceProperties.EnvironmentInterval):
                    if (TryGetValueFromProperty<TimeSpan>(property.Value, out var desiredEnvironmentInterval))
                    {
                        deviceProperties.EnvironmentInterval = desiredEnvironmentInterval;
                        environmentTimer?.Change(TimeSpan.Zero, deviceProperties.EnvironmentInterval);
                        reportedProperties[nameof(DeviceProperties.EnvironmentInterval)] = property.Value;

                        Console.WriteLine("Environment interval set to {0}", deviceProperties.EnvironmentInterval);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse environment interval from value {0}", property.Value);
                    }
                    break;

                default:
                    Console.WriteLine("Unknown device property {0}: {1}", property.Key, property.Value);
                    break;
            }
        }

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private bool TryGetValueFromProperty<T>(object propertyValue, out T? value)
    {
        if (propertyValue is JValue jsonValue)
        {
            value = jsonValue.ToObject<T>();
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    private async Task ReportCurrentPropertiesAsync()
    {
        var reportedProperties = new TwinCollection();

        reportedProperties[nameof(deviceProperties.CameraInterval)] = deviceProperties.CameraInterval;
        reportedProperties[nameof(deviceProperties.EnvironmentInterval)] = deviceProperties.EnvironmentInterval;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }
}