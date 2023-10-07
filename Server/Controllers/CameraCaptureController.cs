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
public class CameraCaptureController : ControllerBase
{
    private readonly CameraCaptureService cameraCaptureService;
    private readonly ILogger<CameraCaptureController> logger;

    public CameraCaptureController(
        CameraCaptureService cameraCaptureService,
        ILogger<CameraCaptureController> logger)
    {
        this.cameraCaptureService = cameraCaptureService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<CapturedImage>> GetAsync(string date, string cameraLabel)
    {
        return await cameraCaptureService.GetCameraCaptures(DateOnly.ParseExact(date, CapturedImageNaming.DatePathFormat), cameraLabel);
    }

    [HttpGet("Image")]
    public async Task<ActionResult> GetImageAsync(string blob, CancellationToken cancellationToken)
    {
        var imageContent = await cameraCaptureService.GetCapturedImage(blob, cancellationToken);

        if (imageContent is null)
        {
            return NotFound();
        }

        return new FileContentResult(imageContent, "image/jpeg");
    }
}
