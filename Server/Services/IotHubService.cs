using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using PiPanel.Server.Options;
using PiPanel.Shared;
using PiPanel.Shared.Camera;
using System.Text.Json;

namespace PiPanel.Server.Services;

public class IotHubService
{
    private ServiceClient serviceClient;
    private RegistryManager registryManager;
    private readonly IotOptions options;
    private readonly ILogger<IotHubService> logger;

    public IotHubService(
        IOptions<IotOptions> optionsSnapshot,
        ILogger<IotHubService> logger)
    {
        this.logger = logger;

        options = optionsSnapshot.Value;

        serviceClient = ServiceClient.CreateFromConnectionString(options.ServiceConnectionString);
        registryManager = RegistryManager.CreateFromConnectionString(options.ManagerConnectionString);
    }

    public async Task<PiPanelStatus> GetDevice()
    {
        var deviceTwin = await registryManager.GetTwinAsync(options.DeviceName);

        return new PiPanelStatus
        {
            IsConnected = deviceTwin.ConnectionState == DeviceConnectionState.Connected,
            LastActivityTime = deviceTwin.LastActivityTime,
        };
    }

    public async Task<DeviceProperties> GetDeviceProperties()
    {
        var deviceTwin = await registryManager.GetTwinAsync(options.DeviceName);
        var reportedPropertiesJson = deviceTwin.Properties.Reported.ToJson();

        return JsonSerializer.Deserialize<DeviceProperties>(reportedPropertiesJson)
            ?? throw new ApplicationException("Unable to deserialize reported device properties");
    }

    public async Task SetDeviceCamera(string cameraKey, CameraInfo camera)
    {
        var desiredCameraProperties = new TwinCollection();
        desiredCameraProperties[cameraKey] = new TwinCollection(JsonSerializer.Serialize(camera));

        await SetDeviceProperty(nameof(DeviceProperties.Cameras), desiredCameraProperties);
    }

    public async Task SetDeviceProperty(string key, object value)
    {
        var deviceTwin = await registryManager.GetTwinAsync(options.DeviceName);

        var desiredProperties = new TwinCollection();
        desiredProperties[key] = value;

        var desiredPropertyUpdate = new Twin
        {
            Properties = new TwinProperties
            {
                Desired = desiredProperties,
            },
        };

        await registryManager.UpdateTwinAsync(deviceTwin.DeviceId, desiredPropertyUpdate, deviceTwin.ETag);
    }
}
