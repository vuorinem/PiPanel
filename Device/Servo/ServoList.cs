namespace PiPanel.Device.Servo;

public static class ServoList
{
    public static readonly IList<ServoInfo> Servos = new List<ServoInfo>() {
        new() {
            Frequency = 50,
            MinimumPulseWidthMilliseconds = 800,
            MaximumPulseWidthMilliseconds = 2000,
            MaximumAngle = 180,
        },
    };
}