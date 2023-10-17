namespace PiPanel.Device.Servo;

public class ServoInfo
{
    public int Frequency { get; set; }

    public short MinimumPulseWidthMilliseconds { get; set; }

    public short MaximumPulseWidthMilliseconds { get; set; }

    public short MaximumAngle { get; set; }
}