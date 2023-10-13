using Microsoft.Azure.Devices.Client;

namespace PiPanel.Device.Servo;

public class ServoService : IDisposable
{
    private readonly ServoController controller;
    private readonly DeviceClient deviceClient;

    public ServoService(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;

        controller = new ServoController(ServoList.Servos[0]);

        controller.Start();
    }

    public void SetAngle(double angle)
    {
        controller.SetAngle(angle);
    }

    public void Dispose()
    {
        controller.Stop();
        controller.Dispose();
    }
}