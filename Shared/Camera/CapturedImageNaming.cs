namespace PiPanel.Shared.Camera;

public static class CapturedImageNaming
{
    public const string DatePathFormat = "yyyy-MM-dd";
    public const string TimePathFormat = "HH-mm-ss";

    public static string GetBlobNameForDevice(string cameraKey, DateTime capturedAt)
    {
        var datePath = capturedAt.ToString(DatePathFormat);
        var timePath = capturedAt.ToString(TimePathFormat);

        return $"{cameraKey}/{datePath}/{timePath}.jpg";
    }

    public static string GetBlobPrefixForDate(string deviceName, string cameraKey, DateOnly date)
    {
        var datePath = date.ToString(DatePathFormat);

        return $"{deviceName}/{cameraKey}/{datePath}/";
    }
}
