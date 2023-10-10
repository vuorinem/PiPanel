using PiPanel.Shared.Camera;

namespace PiPanel.Shared;

public class DeviceProperties
{
    public IList<CameraInfo> Cameras { get; set; } = default!;

    public TimeSpan CameraInterval { get; set; }

    public TimeSpan EnvironmentInterval { get; set; }
}
