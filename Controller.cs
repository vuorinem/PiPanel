using Microsoft.Azure.Devices.Client;

public class Controller
{
    private readonly DeviceClient deviceClient;

    private CancellationToken shutdownToken;

    public Controller(CancellationToken shutdownToken, DeviceClient deviceClient)
    {
        this.shutdownToken = shutdownToken;
        this.deviceClient = deviceClient;
    }

    public async Task RunAsync()
    {
        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);

        while (!shutdownToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
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