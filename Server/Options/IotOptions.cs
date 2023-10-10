namespace PiPanel.Server.Options;

public class IotOptions
{
    public string ServiceConnectionString { get; set; } = default!;

    public string ManagerConnectionString { get; set; } = default!;

    public string HubName { get; set; } = default!;

    public string DeviceName { get; set; } = default!;
}
