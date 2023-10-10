namespace PiPanel.Shared;

public class DeviceIntervalUpdateRequest
{
    public string Key { get; set; } = default!;

    public int ValueInSeconds { get; set; } = default!;
}
