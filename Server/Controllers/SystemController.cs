using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;
using PiPanel.Server.Options;

namespace PiPanel.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope("API.Access")]
public class SystemController : ControllerBase
{
    private readonly SystemOptions options;
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        IOptionsSnapshot<SystemOptions> optionsSnapshot,
        ILogger<SystemController> logger)
    {
        options = optionsSnapshot.Value;

        _logger = logger;
    }

    [HttpGet("SyncfusionKey")]
    public string GetSyncfusionKeyAsync()
    {
        return options.SyncfusionLicenseKey;
    }
}
