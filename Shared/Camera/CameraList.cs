namespace PiPanel.Shared.Camera;

public static class CameraList
{
    public static readonly ICollection<CameraInfo> Cameras = new CameraInfo[]
    {
        new() {
            Label = "PiCam",
            Source = "/dev/video0",
            ResolutionWidth = 640,
            ResolutionHeight = 480,
        },
        new() {
            Label = "WebCam",
            Source = "/dev/video1",
            ResolutionWidth = 640,
            ResolutionHeight = 480,
        },
    };
}
