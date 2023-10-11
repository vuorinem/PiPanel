namespace PiPanel.Shared.Environment;

public static class EnvironmentStatusNaming
{
    public const string DatePathFormat = "yyyy-MM-dd";
    public const string TimePathFormat = "HH-mm";

    public static string GetBlobPrefixForDate(string hubName, DateOnly date)
    {
        var datePath = date.ToString(DatePathFormat);

        return $"{hubName}/{datePath}/";
    }
    public static string GetAggregateBlobNameForDate(string hubName, DateOnly date)
    {
        var datePath = date.ToString(DatePathFormat);

        return $"{hubName}/{datePath}.json";
    }
}
