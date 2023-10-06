using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PiPanel.Shared;

namespace PiPanel.Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[RequiredScope("API.Access")]
public class CameraCaptureController : ControllerBase
{
    private readonly ILogger<CameraCaptureController> _logger;

    public CameraCaptureController(ILogger<CameraCaptureController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
