using System.Text.Json;
using PiPanel.Device;

Console.WriteLine("* * * * *");
Console.WriteLine("Starting up PiPanel");

var config = JsonSerializer.Deserialize<PiPanelConfig>(File.ReadAllBytes("config.json"));

if (config is null)
{
    Console.WriteLine("Error reading configuration");
    Environment.Exit(1);
}

Console.WriteLine("Connecting to hub");

using var controller = new Controller(config);

// Add shutdown handler from keyboard
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Shutdown requested by user");

    eventArgs.Cancel = true;
    controller.Stop();
};

// Add shutdown handler from parent process (e.g. systemctl)
AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
{
    Console.WriteLine("Shutdown requested by parent process");
    controller.Stop();
};

Console.WriteLine("Start up complete");

try
{
    await controller.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected exception: {ex.Message}");
    Console.Error.WriteLine(ex);

    Environment.Exit(1);
}

Console.WriteLine("Closing hub connection");

Console.WriteLine("PiPanel shutdown complete");
Console.WriteLine("* * * * *");
