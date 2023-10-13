using Microsoft.Azure.Devices.Client;
using PiPanel.Device.Environment;

namespace PiPanel.Device.Servo;

public class ServoService
{
    private readonly ServoController controller;
    private readonly DeviceClient deviceClient;
    
    private double angle = 0;

    public ServoService(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;

        controller = new ServoController();
        controller.Start();
    }

    public async void ExecuteAsync(object? state)
    {
        await Task.CompletedTask;

        angle = (angle + 10) % 180;
        
        Console.WriteLine("Setting angle to {0}", angle);

        controller.SetAngle(angle);
    }
}