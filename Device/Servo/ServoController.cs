using System.Device.Pwm;
using Iot.Device.ServoMotor;

namespace PiPanel.Device.Environment;

public class ServoController : IDisposable
{
    private ServoMotor motor;

    public ServoController()
    {
        Console.WriteLine("Setting up servo control channel");

        motor = new ServoMotor(PwmChannel.Create(0, 0, 50), 180, 1000, 2000);
    }

    public void Start()
    {
        motor.Start();
    }

    public void Stop()
    {
        motor.Stop();
    }

    public void SetAngle(double angle)
    {
        motor.WriteAngle(angle);
    }

    public void Dispose()
    {
        motor.Dispose();
    }
}