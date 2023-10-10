# Pi Panel

This is a hobby project for building a Raspberry Pi based home station and
an online panel. The code is C# / .NET full stack, including the device
application and web front-end.

## Functionality

Current functionality includes:

- Camera images
- Temperature and humidity tracking

The functionality is likely to be expanded to include things that are useful,
interesting to build or just fun challenges. Some of the future ideas include:

- Use a display on the Pi to show temperature/humidity locally
- Add LED indicators on Pi for connectivity / picture timings
- Servos to remotely control camera angles
- Direct access to video feed from camera

## Architecture

The system consists of following parts:

- A Raspberry Pi with attached cameras and sensors
- Device application built for Raspberry Pi with .NET
- Azure IoT Hub where the device is registered and managed
- Azure Blob Storage where the data is persisted
- An admin application with a .NET Web API back-end and Blazor front-end

## Known Issues

- There are currently some issues with Raspberry Pi (3) and .NET 7 when
using the IoT libraries, so I have decided to use .NET 6 at least for now.