using System.Device.Pwm;
using Iot.Device.ServoMotor;

namespace PiPanel.Device.Servo;

public class ServoController : IDisposable
{
    private ServoMotor motor;

    public ServoController(ServoInfo servo)
    {
        Console.WriteLine("Setting up servo control channel");

        motor = new ServoMotor(
            PwmChannel.Create(0, 0, servo.Frequency),
            servo.MaximumAngle,
            servo.MinimumPulseWidthMilliseconds,
            servo.MaximumPulseWidthMilliseconds);
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