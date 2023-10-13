using PiPanel.Shared.Camera;

namespace PiPanel.Device.Camera;

public static class CameraList
{
    public static readonly IList<CameraInfo> DefaultCameras = new CameraInfo[]
    {
        new() {
            Label = "PiCam",
            Source = "/dev/video0",
            ResolutionWidth = 640,
            ResolutionHeight = 480,
            IsFlippedVertically = true,
        },
        new() {
            Label = "WebCam",
            Source = "/dev/video1",
            ResolutionWidth = 640,
            ResolutionHeight = 480,
        },
    };
}
