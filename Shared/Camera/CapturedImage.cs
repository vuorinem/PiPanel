namespace PiPanel.Shared.Camera;

public class CapturedImage
{
    public DateTime CreatedAt { get; set; }

    public string BlobName { get; set; } = default!;
}
