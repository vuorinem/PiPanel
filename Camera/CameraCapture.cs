using System.Diagnostics;

public static class CameraCapture
{
    public static readonly ICollection<CameraInfo> Cameras = new CameraInfo[]
    {
        new() {
            Label = "PiCam",
            Source = "/dev/video0",
            ResolutionWidth = 1920,
            ResolutionHeight = 1080,
        },
        new() {
            Label = "WebCam",
            Source = "/dev/video1",
            ResolutionWidth = 1920,
            ResolutionHeight = 1080,
        },
    };

    private static readonly SemaphoreSlim captureLock = new(1, 1);

    public static async Task CaptureImage(CameraInfo camera, string filePath)
    {
        await RunCaptureProcess(camera, filePath);
    }

    private static async Task RunCaptureProcess(CameraInfo camera, string filePath)
    {
        await captureLock.WaitAsync();

        try
        {

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "fswebcam",
                Arguments = $"-r {camera.ResolutionWidth}x{camera.ResolutionHeight} {filePath} -d {camera.Source}",
            });

            if (process is null)
            {
                throw new CameraException("Unable to start fswebcam to capture image");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();

                throw new CameraException($"Capturing image with fswebcam returned error code {process.ExitCode}: {error}");
            }
        }
        finally
        {
            captureLock.Release();
        }
    }
}