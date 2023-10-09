namespace PiPanel.Server.Models;

public class IotHubEvent<T> where T : class
{
    public DateTime EnqueuedTimeUtc { get; set; } = default!;
    public Properties Properties { get; set; } = default!;
    public Systemproperties SystemProperties { get; set; } = default!;
    public T Body { get; set; } = default!;
}

public class Properties
{
    public string Type { get; set; } = default!;
}

public class Systemproperties
{
    public string correlationId { get; set; } = default!;
    public string connectionDeviceId { get; set; } = default!;
    public string connectionAuthMethod { get; set; } = default!;
    public string connectionDeviceGenerationId { get; set; } = default!;
    public string contentType { get; set; } = default!;
    public string contentEncoding { get; set; } = default!;
    public DateTime enqueuedTime { get; set; }
}
