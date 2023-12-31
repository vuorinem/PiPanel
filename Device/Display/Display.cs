using System.Device.Gpio;
using Iot.Device.Multiplexing;

namespace PiPanel.Device.Display;

public class DisplayController
{
    private const int PIN_Data = 16;
    private const int PIN_Clock = 20;
    private const int PIN_Latch = 21;
    private const int PIN_D1 = 5;
    private const int PIN_D2 = 6;
    private const int PIN_D3 = 13;
    private const int PIN_D4 = 26;

    private readonly GpioController gpioController;
    private readonly ShiftRegister shiftRegister;

    private readonly GpioPin[] displayPins;
    private readonly GpioPin latchPin;

    private CancellationTokenSource? displayCancellation;

    private int nextScene = 1;
    private int? currentScene = null;

    public bool IsShowing => displayCancellation is not null || currentScene is not null;

    public DisplayController()
    {
        gpioController = new GpioController();

        displayPins = new[] {
            gpioController.OpenPin(PIN_D1, PinMode.Output, PinValue.Low),
            gpioController.OpenPin(PIN_D2, PinMode.Output, PinValue.Low),
            gpioController.OpenPin(PIN_D3, PinMode.Output, PinValue.Low),
            gpioController.OpenPin(PIN_D4, PinMode.Output, PinValue.Low),
        };

        latchPin = gpioController.OpenPin(PIN_Latch);

        shiftRegister = new ShiftRegister(new ShiftRegisterPinMapping(PIN_Data, PIN_Clock, PIN_Latch), 8);
        shiftRegister.ShiftByte(Segments.Empty);
    }

    public void Clear()
    {
        foreach (var displayPin in displayPins)
        {
            displayPin.Write(PinValue.Low);
        }
    }

    public int? StartScene()
    {
        if (currentScene is not null)
        {
            return null;
        }

        currentScene = nextScene++;

        return currentScene;
    }

    public void StopScene(int scene)
    {
        if (scene != currentScene)
        {
            throw new ApplicationException($"Trying to stop scene {scene} but current scene is {currentScene}");
        }

        currentScene = null;
    }

    public byte[] GetDisplayBytes(string text)
    {
        return Segments.GetDisplaysForText(text, displayPins.Length);
    }

    public byte[] GetDisplayBytes(double number, char? unitCharacter = null)
    {
        if (unitCharacter is not null)
        {
            return Segments.GetDisplaysForText(number.ToString(), displayPins.Length - 1)
                .Append(Segments.GetForCharacter(unitCharacter.Value))
                .ToArray();
        }
        else
        {
            return Segments.GetDisplaysForText(number.ToString(), displayPins.Length);
        }
    }

    public Task RunForAsync(byte[] segmentBytesPerDigit, TimeSpan runFor, int? scene = null)
    {
        return Task.Run(() => RunFor(segmentBytesPerDigit, runFor, scene));
    }

    private void RunFor(byte[] segmentBytesPerDigit, TimeSpan runFor, int? scene = null)
    {
        if (currentScene is not null)
        {
            if (scene is null)
            {
                return;
            }
            else if (scene != currentScene)
            {
                throw new ApplicationException($"Trying to run display for scene {scene} but current scene is {currentScene}");
            }
        }

        try
        {
            StopCurrentDisplay();

            using (displayCancellation = new CancellationTokenSource())
            {
                var cancellationToken = displayCancellation.Token;

                var stopAt = DateTime.Now + runFor;

                while (!cancellationToken.IsCancellationRequested && DateTime.Now < stopAt)
                {
                    for (int displayIndex = 0; displayIndex < displayPins.Length; displayIndex++)
                    {
                        ShowByte(displayIndex, segmentBytesPerDigit[displayIndex]);

                        Thread.Sleep(5);
                    }

                    Clear();
                }
            }
        }
        catch (TaskCanceledException)
        {
            Clear();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during display: {ex.Message}");
        }
        finally
        {
            displayCancellation = null;
        }
    }

    private void StopCurrentDisplay()
    {
        if (displayCancellation is not null)
        {
            displayCancellation.Cancel();
            displayCancellation.Dispose();
            displayCancellation = null;
        }
    }

    private void ShowByte(int currentDisplayIndex, byte byteValue)
    {
        // Prep character in shift register
        latchPin.Write(PinValue.Low);
        shiftRegister.ShiftByte(byteValue, false);

        // Prep correct display to receive the character
        for (int displayIdex = 0; displayIdex < displayPins.Length; displayIdex++)
        {
            displayPins[displayIdex].Write(displayIdex == currentDisplayIndex ? PinValue.High : PinValue.Low);
        }

        // Show character in display
        latchPin.Write(PinValue.High);
        latchPin.Write(PinValue.Low);
    }
}