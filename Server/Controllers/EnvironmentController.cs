using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PiPanel.Server.Services;
using PiPanel.Shared.Camera;
using PiPanel.Shared.Environment;

namespace PiPanel.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope("API.Access")]
public class EnvironmentController : ControllerBase
{
    private readonly EnvironmentService environmentService;
    private readonly ILogger<EnvironmentController> _logger;

    public EnvironmentController(
        EnvironmentService environmentService,
        ILogger<EnvironmentController> logger)
    {
        this.environmentService = environmentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<EnvironmentStatus?> GetLatestForDateAsync(string date)
    {
        return await environmentService.GetLatestForDayAsync(DateOnly.ParseExact(date, EnvironmentStatusNaming.DatePathFormat));
    }
}
