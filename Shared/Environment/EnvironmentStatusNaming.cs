namespace PiPanel.Shared.Environment;

public static class EnvironmentStatusNaming
{
    public const string HubName = "mv-pipanel-hub";
    public const string Partition = "01";
    public const string DatePathFormat = "yyyy-MM-dd";
    public const string TimePathFormat = "HH-mm";

    public static string GetBlobPrefixForDate(DateOnly date)
    {
        var datePath = date.ToString(DatePathFormat);

        return $"{HubName}/{Partition}/{datePath}/";
    }

    public static string GetBlobNameForDateTime(string cameraLabel, DateTime dateTime)
    {
        var datePath = dateTime.ToString(DatePathFormat);
        var timePath = dateTime.ToString(TimePathFormat);

        return $"{HubName}/{Partition}/{datePath}/{timePath}.JSON";
    }
}
