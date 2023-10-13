using PiPanel.Shared.Camera;

namespace PiPanel.Device.Camera;

public static class CameraList
{
    public static readonly IDictionary<string, CameraInfo> DefaultCameras = new Dictionary<string, CameraInfo>
    {
        {
            "PiCam",
            new() {
                Label = "PiCam",
                Source = "/dev/video0",
                ResolutionWidth = 640,
                ResolutionHeight = 480,
                IsFlippedVertically = true,
            }
        },
        {
            "WebCam",
            new() {
                Label = "WebCam",
                Source = "/dev/video1",
                ResolutionWidth = 640,
                ResolutionHeight = 480,
            }
        },
    };
}
