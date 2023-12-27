using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using PiPanel.Device.Camera;
using PiPanel.Device.Display;
using PiPanel.Device.Environment;
using PiPanel.Device.Servo;
using PiPanel.Device.Weather;
using PiPanel.Shared;
using PiPanel.Shared.Camera;

namespace PiPanel.Device;

public class Controller : IDisposable
{
    private readonly DeviceClient deviceClient;

    private readonly DeviceProperties deviceProperties;

    private readonly PiPanelConfig config;

    private readonly TimeSpan controllerSleepInterval = TimeSpan.FromMinutes(1);

    private readonly TimeSpan DefaultCameraInterval = TimeSpan.FromMinutes(20);

    private readonly TimeSpan DefaultEnvironmentInterval = TimeSpan.FromMinutes(5);

    private const short DefaultAngle = 90;

    private const short DefaultAutoRotateAngle = 0;

    private const short DefaultCameraTimerSeconds = 5;

    private bool isRunning = false;

    private CancellationTokenSource? controllerSleepTokenSource;

    private Timer? cameraTimer;

    private Timer? environmentTimer;

    private ServoService? servoService;

    private SemaphoreSlim weatherSemaphore = new(1, 1);

    private bool disposedValue;

    public Controller(PiPanelConfig config)
    {
        deviceClient = DeviceClient.CreateFromConnectionString(config.DeviceConnectionString);

        // Initialize with default properties
        deviceProperties = new DeviceProperties
        {
            Cameras = CameraList.DefaultCameras,
            CameraInterval = DefaultCameraInterval,
            EnvironmentInterval = DefaultEnvironmentInterval,
            Angle = DefaultAngle,
            AutoRotateAngle = DefaultAutoRotateAngle,
            CameraTimerSeconds = DefaultCameraTimerSeconds,
        };

        this.config = config;
    }

    public async Task RunAsync()
    {
        isRunning = true;

        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);
        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null);

        await ReportCurrentPropertiesAsync();

        var environmentService = new EnvironmentService(deviceClient);
        environmentTimer = new Timer(environmentService.ExecuteAsync, null, TimeSpan.Zero, deviceProperties.EnvironmentInterval);

        try
        {
            servoService = new ServoService(deviceClient);
            servoService.SetAngle(deviceProperties.Angle);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unable to start servo service, servo will not be available: {0}", ex.Message);
            servoService = null;
        }

        var display = new DisplayController();
        _ = Task.Run(() => RunBackgroundDisplay(display, environmentService));

        var cameraService = new CameraService(deviceClient, deviceProperties);
        cameraTimer = new Timer(
            async (object? state) =>
            {
                if (deviceProperties.CameraTimerSeconds > 0)
                {
                    await Animations.AnimateCountdownAsync(display, deviceProperties.CameraTimerSeconds);
                }

                await cameraService.ExecuteAsync(state);

                if (deviceProperties.AutoRotateAngle > 0)
                {
                    deviceProperties.Angle = (short)
                        ((deviceProperties.Angle + deviceProperties.AutoRotateAngle) % (ServoList.Servos[0].MaximumAngle + 1));

                    servoService?.SetAngle(deviceProperties.Angle);

                    await ReportCurrentPropertyAsync(nameof(DeviceProperties.Angle), deviceProperties.Angle);
                }
            },
            null, TimeSpan.Zero, deviceProperties.CameraInterval);

        var weatherService = new WeatherService(deviceProperties, config.WeatherApiKey);

        var obstacleSensor = new ObstacleSensor(
            async () =>
            {
                await DisplayWeather(display, weatherService);
            },
            () => { }
        );

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
                isRunning = false;
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
        servoService?.Dispose();

        Console.WriteLine("Controller stopped");
    }

    public void Stop()
    {
        isRunning = false;
        controllerSleepTokenSource?.Cancel();
    }

    private async Task RunBackgroundDisplay(DisplayController display, EnvironmentService environmentService)
    {
        var displayContent = 0;

        while (isRunning)
        {
            try
            {
                if (display.IsShowing)
                {
                    await Task.Delay(1000);
                }
                else if (displayContent == 0 && environmentService.LatestTemperature is not null)
                {
                    var bytes = display.GetDisplayBytes(environmentService.LatestTemperature.Value, 'C');
                    await display.RunForAsync(bytes, TimeSpan.FromSeconds(2));

                    displayContent = (displayContent + 1) % 3;
                }
                else if (displayContent == 1 && environmentService.LatestHumidity is not null)
                {
                    var bytes = display.GetDisplayBytes(environmentService.LatestHumidity.Value, 'H');
                    await display.RunForAsync(bytes, TimeSpan.FromSeconds(2));

                    displayContent = (displayContent + 1) % 3;
                }
                else
                {
                    var bytes = display.GetDisplayBytes(DateTime.Now.ToString("HH.mm"));
                    await display.RunForAsync(bytes, TimeSpan.FromSeconds(2));

                    displayContent = (displayContent + 1) % 3;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in RunBackgroundDisplay task: {ex.Message}");
            }
        }
    }

    private async Task DisplayWeather(DisplayController display, WeatherService weatherService)
    {
        if (weatherSemaphore.CurrentCount == 0)
        {
            return;
        }

        try
        {
            weatherSemaphore.Wait();

            Console.WriteLine("Retrieving current weather");

            var weather = await weatherService.GetCurrentWeather();

            if (weather is null)
            {
                Console.Error.WriteLine("Unable to retrieve weather");
                return;
            }

            var scene = display.StartScene();

            if (scene is null)
            {
                // Something else is running a display scene, skip weather display
                return;
            }

            try
            {
                var breakIndicatorBytes = new[] { Segments.Center, Segments.Center, Segments.Center, Segments.Center };

                await display.RunForAsync(display.GetDisplayBytes(" -- "), TimeSpan.FromSeconds(1), scene);
                await display.RunForAsync(display.GetDisplayBytes("tenp"), TimeSpan.FromSeconds(1), scene);
                await DisplayNumberOrEmpty(display, scene.Value, weather.Temperature, 'C', TimeSpan.FromSeconds(3));
                await display.RunForAsync(display.GetDisplayBytes("rain"), TimeSpan.FromSeconds(1), scene);
                await DisplayNumberOrEmpty(display, scene.Value, weather.RainProbability * 100, 'r', TimeSpan.FromSeconds(3));
                await display.RunForAsync(display.GetDisplayBytes("uind"), TimeSpan.FromSeconds(1), scene);
                await DisplayNumberOrEmpty(display, scene.Value, weather.WindSpeed, 'u', TimeSpan.FromSeconds(3));
                await display.RunForAsync(display.GetDisplayBytes(" -- "), TimeSpan.FromSeconds(1), scene);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in RunBackgroundDisplay task: {ex.Message}");
            }
            finally
            {
                if (scene is not null)
                {
                    display.StopScene(scene.Value);
                }
            }
        }
        finally
        {
            weatherSemaphore.Release();
        }
    }

    private static async Task DisplayNumberOrEmpty(DisplayController display, int scene, double? number, char? unitCharacter, TimeSpan duration)
    {
        if (number is not null)
        {
            await display.RunForAsync(display.GetDisplayBytes(number.Value, unitCharacter), duration, scene);
        }
        else
        {
            await display.RunForAsync(display.GetDisplayBytes("-   "), duration, scene);
        }
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

                case nameof(DeviceProperties.Location):
                    if (TryGetValueFromProperty<Location>(property.Value, out var desiredLocation))
                    {
                        if (desiredLocation is null)
                        {
                            Console.Error.WriteLine("Desired device properties has invalid null value for location");
                            break;
                        }

                        deviceProperties.Location = desiredLocation;
                        reportedProperties[nameof(DeviceProperties.Location)] = property.Value;

                        Console.WriteLine("Location set to {0}", deviceProperties.Location);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse Location from value {0}", property.Value);
                    }
                    break;

                case nameof(DeviceProperties.Angle):
                    if (TryGetValueFromProperty<short>(property.Value, out var desiredAngle))
                    {
                        deviceProperties.Angle = desiredAngle;

                        servoService?.SetAngle(deviceProperties.Angle);

                        reportedProperties[nameof(DeviceProperties.Angle)] = desiredAngle;

                        Console.WriteLine("Angle set to {0}", deviceProperties.Angle);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse angle from value {0}", property.Value);
                    }
                    break;

                case nameof(DeviceProperties.AutoRotateAngle):
                    if (TryGetValueFromProperty<short>(property.Value, out var desiredAutoRotateAngle))
                    {
                        deviceProperties.AutoRotateAngle = desiredAutoRotateAngle;

                        reportedProperties[nameof(DeviceProperties.AutoRotateAngle)] = desiredAutoRotateAngle;

                        Console.WriteLine("AutoRotateAngle set to {0}", deviceProperties.AutoRotateAngle);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse auto-rotate angle from value {0}", property.Value);
                    }
                    break;

                case nameof(DeviceProperties.CameraTimerSeconds):
                    if (TryGetValueFromProperty<short>(property.Value, out var desiredCameraTimerSeconds))
                    {
                        deviceProperties.CameraTimerSeconds = desiredCameraTimerSeconds;

                        reportedProperties[nameof(DeviceProperties.CameraTimerSeconds)] = desiredCameraTimerSeconds;

                        Console.WriteLine("CameraTimerSeconds set to {0}", deviceProperties.CameraTimerSeconds);
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse camera timer seconds from value {0}", property.Value);
                    }
                    break;

                case nameof(DeviceProperties.Cameras):
                    if (TryGetValueFromProperty<IDictionary<string, CameraInfo?>>(property.Value, out var cameras))
                    {
                        if (cameras is null)
                        {
                            Console.Error.WriteLine("Desired device properties has invalid null value for cameras");
                            break;
                        }

                        foreach (var cameraItem in cameras)
                        {
                            if (cameraItem.Value is not null)
                            {
                                if (deviceProperties.Cameras.ContainsKey(cameraItem.Key))
                                {
                                    Console.WriteLine("Updating Camera {0}", cameraItem.Key);
                                }
                                else
                                {
                                    Console.WriteLine("Adding new Camera {0}", cameraItem.Key);
                                }

                                deviceProperties.Cameras[cameraItem.Key] = cameraItem.Value;
                            }
                            else if (deviceProperties.Cameras.ContainsKey(cameraItem.Key))
                            {
                                Console.WriteLine("Removing camera {0}", cameraItem.Key);
                                deviceProperties.Cameras.Remove(cameraItem.Key);
                            }
                        }

                        reportedProperties[nameof(DeviceProperties.Cameras)] = deviceProperties.Cameras;
                    }
                    else
                    {
                        Console.WriteLine("Unable to parse camera info from value {0}", property.Value);
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
        if (propertyValue is JToken jsonToken)
        {
            value = jsonToken.ToObject<T>();
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

        reportedProperties[nameof(DeviceProperties.CameraInterval)] = deviceProperties.CameraInterval;
        reportedProperties[nameof(DeviceProperties.EnvironmentInterval)] = deviceProperties.EnvironmentInterval;
        reportedProperties[nameof(DeviceProperties.Cameras)] = deviceProperties.Cameras;
        reportedProperties[nameof(DeviceProperties.Angle)] = deviceProperties.Angle;
        reportedProperties[nameof(DeviceProperties.AutoRotateAngle)] = deviceProperties.AutoRotateAngle;
        reportedProperties[nameof(DeviceProperties.CameraTimerSeconds)] = deviceProperties.CameraTimerSeconds;
        reportedProperties[nameof(DeviceProperties.Location)] = deviceProperties.Location;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private async Task ReportCurrentPropertyAsync(string propertyName, dynamic propertyValue)
    {
        var reportedProperties = new TwinCollection();

        reportedProperties[propertyName] = propertyValue;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                deviceClient.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}