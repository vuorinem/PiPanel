using Microsoft.Azure.Devices.Client;

namespace PiPanel.Device.Servo;

public class ServoService
{
    private readonly ServoController controller;
    private readonly DeviceClient deviceClient;

    public ServoService(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;

        controller = new ServoController(ServoList.Servos[0]);
    }
}