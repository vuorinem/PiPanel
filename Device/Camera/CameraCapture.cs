using PiPanel.Shared.Camera;
using System.Diagnostics;

namespace PiPanel.Device.Camera;

public static class CameraCapture
{
    private static readonly SemaphoreSlim captureLock = new(1, 1);

    public static async Task CaptureImageAsync(CameraInfo camera, string filePath)
    {
        await RunCaptureProcessAsync(camera, filePath);
    }

    private static async Task RunCaptureProcessAsync(CameraInfo camera, string filePath)
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