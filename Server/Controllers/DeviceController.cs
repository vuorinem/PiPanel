using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PiPanel.Server.Services;
using PiPanel.Shared;

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
}
