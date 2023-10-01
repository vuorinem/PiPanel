using System;
using System.Device.Gpio;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

using var controller = new GpioController();

controller.OpenPin(18, PinMode.Output);

bool ledOn = true;

while (true)
{
    controller.Write(18, ledOn ? PinValue.High : PinValue.Low);
    Thread.Sleep(1000);
    ledOn = !ledOn;
}