using Microsoft.Azure.Devices;
using Microsoft.Extensions.Options;
using PiPanel.Server.Options;
using PiPanel.Shared;

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
        var deviceTwin = await registryManager.GetTwinAsync(PiPanelStatus.DeviceName);

        return new PiPanelStatus
        {
            IsConnected = deviceTwin.ConnectionState == DeviceConnectionState.Connected,
            LastActivityTime = deviceTwin.LastActivityTime,
        };
    }
}
