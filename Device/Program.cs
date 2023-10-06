using System.Text.Json;
using Microsoft.Azure.Devices.Client;

Console.WriteLine("Starting up PiPanel...");

using CancellationTokenSource cancellationTokenSource = new();

Console.WriteLine("> reading configuration");

var config = JsonSerializer.Deserialize<PiPanelConfig>(File.ReadAllBytes("config.json"));

if (config is null)
{
    Environment.Exit(1);
}

Console.WriteLine("> connecting to hub");
using var deviceClient = DeviceClient.CreateFromConnectionString(config.DeviceConnectionString);

var controller = new Controller(deviceClient);

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    controller.Stop();
    Console.WriteLine("Shutdown requested");
};

Console.WriteLine("Start up complete");

try
{
    await controller.RunAsync();
}
catch
{
    Console.WriteLine();

    Environment.Exit(1);
}

Console.WriteLine("Shutting down PiPanel...");

Console.WriteLine("> closing hub connection");
await deviceClient.CloseAsync();

Console.WriteLine("Shutdown complete");
