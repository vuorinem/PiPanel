namespace PiPanel.Device.Display;

public static class Animations
{
    public record DisplayState(TimeSpan ShowFor, byte[] DisplayBytes);

    public static DisplayState[] Countdown = new DisplayState[]
    {
        new( TimeSpan.FromMilliseconds(500), new byte[] {
            (byte)(Segments.LeftTop & Segments.LeftBottom),
            Segments.Empty,
            Segments.Empty,
            (byte)(Segments.RightTop & Segments.RightBottom),
        }),
        new (TimeSpan.FromMilliseconds(500), new byte[] {
            (byte)(Segments.RightTop & Segments.RightBottom),
            Segments.Empty,
            Segments.Empty,
            (byte)(Segments.LeftTop & Segments.LeftBottom),
        }),
        new (TimeSpan.FromMilliseconds(500), new byte[] {
            Segments.Empty,
            (byte)(Segments.LeftTop & Segments.LeftBottom),
            (byte)(Segments.RightTop & Segments.RightBottom),
            Segments.Empty,
        }),
        new (TimeSpan.FromMilliseconds(500), new byte[] {
            Segments.Empty,
            (byte)(Segments.RightTop & Segments.RightBottom),
            (byte)(Segments.LeftTop & Segments.LeftBottom),
            Segments.Empty,
        }),
        new (TimeSpan.FromMilliseconds(200), new byte[] {
            Segments.Full,
            Segments.Full,
            Segments.Full,
            Segments.Full,
        }),
        new (TimeSpan.FromMilliseconds(200), new byte[] {
            Segments.Empty,
            Segments.Empty,
            Segments.Empty,
            Segments.Empty,
        }),
        new (TimeSpan.FromMilliseconds(200), new byte[] {
            Segments.Full,
            Segments.Full,
            Segments.Full,
            Segments.Full,
        }),
        new (TimeSpan.FromMilliseconds(200), new byte[] {
            Segments.Empty,
            Segments.Empty,
            Segments.Empty,
            Segments.Empty,
        }),
    };

    public static void Animate(DisplayController display, DisplayState[] animation)
    {
        foreach (var scene in animation)
        {
            display.RunFor(scene.DisplayBytes, scene.ShowFor);
        }
    }
}