﻿using PiPanel.Shared.Camera;

namespace PiPanel.Shared;

public class DeviceProperties
{
    public IDictionary<string, CameraInfo> Cameras { get; set; } = default!;

    public TimeSpan CameraInterval { get; set; }

    public TimeSpan EnvironmentInterval { get; set; }

    public short Angle { get; set; }

    public short AutoRotateAngle { get; set; }

    public short CameraTimerSeconds { get; set; }

    public Location Location { get; set; } = default!;
}
