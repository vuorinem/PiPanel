namespace PiPanel.Shared.Camera;

public static class CapturedImageNaming
{
    public const string DatePathFormat = "yyyy-MM-dd";
    public const string TimePathFormat = "HH-mm-ss";

    public static string GetBlobNameForDevice(string cameraLabel, DateTime capturedAt)
    {
        var datePath = capturedAt.ToString(DatePathFormat);
        var timePath = capturedAt.ToString(TimePathFormat);

        return $"{cameraLabel}/{datePath}/{timePath}.jpg";
    }

    public static string GetBlobPrefixForDate(string deviceName, string cameraLabel, DateOnly date)
    {
        var datePath = date.ToString(DatePathFormat);

        return $"{deviceName}/{cameraLabel}/{datePath}/";
    }
}
