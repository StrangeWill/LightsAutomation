using System.Device.Gpio;
using Lights.Web.AddHostedService;
using Lights.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lights.Web.Controllers;

[Route("api/v0/songs")]
public class SongController(
    FileService fileService,
    LightService lightService) : Controller
{
    protected LightService LightService { get; } = lightService;
    protected FileService FileService { get; } = fileService;

    [HttpPost("start")]
    public async Task<IActionResult> Start(string song)
    {
        LightService.Play = true;
        LightService.SongFile = song;
        return Ok();
    }


    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        LightService.Play = false;
        return Ok();
    }

    [HttpGet("list")]
    public IActionResult List()
    {
        return Ok(FileService.GetFiles());
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (!file.FileName.EndsWith(".mid"))
        {
            return BadRequest(new
            {
                Error = "Only accepting .mid files, plz"
            });
        }

        using var fileStream = file.OpenReadStream();
        await FileService.Save(file.FileName, fileStream);
        return Ok();
    }
}
