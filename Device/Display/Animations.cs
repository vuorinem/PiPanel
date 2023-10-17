namespace PiPanel.Device.Display;

public static class Animations
{
    public record DisplayState(TimeSpan ShowFor, byte[] DisplayBytes);

    public static void AnimateCountdown(DisplayController display, int seconds)
    {
        if (seconds > 9)
        {
            throw new ArgumentOutOfRangeException("Countdown only supports up to 9 seconds");
        }

        var displayStates = new List<DisplayState>();

        for (int second = seconds; second > 0; second--)
        {
            displayStates.AddRange(new DisplayState[] {
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Top,
                    Segments.Empty,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Empty,
                    Segments.Top,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Empty,
                    Segments.RightTop,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Empty,
                    Segments.RightBottom,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Empty,
                    Segments.Bottom,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.Bottom,
                    Segments.Empty,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.LeftBottom,
                    Segments.Empty,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
                new(TimeSpan.FromMilliseconds(125), new byte[] {
                    Segments.GetForCharacter(second.ToString()[0]),
                    Segments.LeftTop,
                    Segments.Empty,
                    Segments.GetForCharacter(second.ToString()[0]),
                }),
            });
        }

        displayStates.Add(
            new(TimeSpan.FromMilliseconds(1000), new byte[] {
                (byte)(Segments.Bottom & Segments.LeftBottom & Segments.LeftTop & Segments.Top),
                (byte)(Segments.Bottom & Segments.Top),
                (byte)(Segments.Bottom & Segments.Top),
                (byte)(Segments.Bottom & Segments.RightTop & Segments.RightBottom & Segments.Top),
            })
        );

        Animate(display, displayStates);
    }

    public static void Animate(DisplayController display, IEnumerable<DisplayState> animation)
    {
        if (!display.StartScene())
        {
            Console.Error.WriteLine("Could not start animation, display is currently in a scene");
        }

        Console.WriteLine("Starting animation scene");

        foreach (var screen in animation)
        {
            display.RunFor(screen.DisplayBytes, screen.ShowFor, true);
        }

        display.StopScene();

        Console.WriteLine("Animation scene completed");
    }
}