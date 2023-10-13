namespace PiPanel.Shared.Camera;

public class CameraInfo
{
    public string Label { get; set; } = default!;

    public string Source { get; set; } = default!;

    public short ResolutionWidth { get; set; }

    public short ResolutionHeight { get; set; }

    public bool IsFlippedHorizontally { get; set; }

    public bool IsFlippedVertically { get; set; }
}