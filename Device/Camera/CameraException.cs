namespace PiPanel.Device.Camera;

public class CameraException : Exception
{
    public CameraException(string? message) : base(message) { }

    public CameraException(string? message, Exception? innerException) : base(message, innerException) { }
}