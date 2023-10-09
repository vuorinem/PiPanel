using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using PiPanel.Device.Camera;
using PiPanel.Device.Environment;

namespace PiPanel.Device;

public class Controller
{
    private readonly DeviceClient deviceClient;

    private bool isRunning = false;

    private TimeSpan controllerSleepInterval = TimeSpan.FromMinutes(1);

    private CancellationTokenSource? controllerSleepTokenSource;

    private TimeSpan cameraInterval = TimeSpan.FromMinutes(30);

    private Timer? cameraTimer;

    private TimeSpan environmentInterval = TimeSpan.FromMinutes(5);

    private Timer? environmentTimer;

    public Controller(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;
    }

    public async Task RunAsync()
    {
        isRunning = true;

        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);
        await deviceClient.SetMethodHandlerAsync(MethodNames.SetCameraInterval, OnSetCameraIntervalAsync, null);

        var cameraService = new CameraService(deviceClient);
        cameraTimer = new Timer(cameraService.ExecuteAsync, null, TimeSpan.Zero, cameraInterval);

        var environmentService = new EnvironmentService(deviceClient);
        environmentTimer = new Timer(environmentService.ExecuteAsync, null, TimeSpan.Zero, environmentInterval);

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

    private async Task<MethodResponse> OnSetCameraIntervalAsync(MethodRequest methodRequest, object userContext)
    {
        await Task.CompletedTask;

        try
        {
            var intervalInSeconds = JsonSerializer.Deserialize<int>(methodRequest.DataAsJson);

            Console.WriteLine($"Hub set camera interval to {intervalInSeconds} seconds");

            cameraInterval = TimeSpan.FromSeconds(intervalInSeconds);
            cameraTimer?.Change(TimeSpan.Zero, cameraInterval);

            return new MethodResponse(0);
        }
        catch
        {
            Console.WriteLine($"Hub set camera intercal with invalid value '{methodRequest.Data}'");

            return new MethodResponse((int)MethodResponseStatusCode.BadRequest);
        }
    }

    private async Task OnReceiveMessage(Message message, object userContext)
    {
        using var bodyReader = new StreamReader(message.BodyStream);
        var body = await bodyReader.ReadToEndAsync();

        Console.WriteLine($"Received message: {body}");

        await deviceClient.CompleteAsync(message);
    }

    private void OnConnectionStatusChanges(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        Console.WriteLine($"Connection status: {status} ({reason})");
    }
}