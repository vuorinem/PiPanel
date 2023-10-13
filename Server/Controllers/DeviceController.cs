using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PiPanel.Server.Services;
using PiPanel.Shared;
using PiPanel.Shared.Camera;

namespace PiPanel.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope("API.Access")]
public class DeviceController : ControllerBase
{
    private readonly IotHubService iotHubService;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        IotHubService iotHubService,
        ILogger<DeviceController> logger)
    {
        this.iotHubService = iotHubService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<PiPanelStatus> GetAsync()
    {
        return await iotHubService.GetDevice();
    }

    [HttpGet("Properties")]
    public async Task<DeviceProperties> GetDevicePropertiesAsync()
    {
        return await iotHubService.GetDeviceProperties();
    }

    [HttpPost("Interval")]
    public async Task<ActionResult> SetDeviceProperty(DeviceIntervalUpdateRequest request)
    {
        await iotHubService.SetDeviceProperty(request.Key, TimeSpan.FromSeconds(request.ValueInSeconds));

        return NoContent();
    }

    [HttpPost("Cameras/{cameraKey}")]
    public async Task<ActionResult> SetCamera(string cameraKey, CameraInfo camera)
    {
        await iotHubService.SetDeviceCamera(cameraKey, camera);

        return NoContent();
    }
}
