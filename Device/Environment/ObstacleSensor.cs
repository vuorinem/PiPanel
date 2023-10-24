using System.Device.Gpio;

namespace PiPanel.Device.Environment;

public class ObstacleSensor
{
    public bool IsBlocked => pin.Read() == PinValue.High;

    private const int GpioPinNumber = 4;
    private readonly GpioPin pin;
    private readonly GpioController controller;

    public ObstacleSensor(Action onBlocked, Action onUnblocked)
    {
        controller = new GpioController(PinNumberingScheme.Logical);
        pin = controller.OpenPin(GpioPinNumber, PinMode.Input);

        pin.ValueChanged += (sender, args) =>
        {
            var action = args.ChangeType switch
            {
                PinEventTypes.None => null,
                PinEventTypes.Rising => onBlocked,
                PinEventTypes.Falling => onUnblocked,
                _ => null,
            };

            if (action is not null)
            {
                action();
            }
        };
    }
}