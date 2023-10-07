namespace PiPanel.Shared;

public class PiPanelStatus
{
    public const string DeviceName = "RaspberryPi2";

    public bool IsConnected { get; set; }

    public DateTime? LastActivityTime { get; set; }
}
