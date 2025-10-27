using System.Device.Gpio;
using Lights.Web.AddHostedService;
using Microsoft.AspNetCore.Mvc;

namespace Lights.Web.Controllers;

[Route("api/v0/relays")]
public class RelayController : Controller
{
    protected LightService LightService { get; }

    public RelayController(LightService lightService)
    {
        LightService = lightService;
    }

    [HttpPost("{number}/open")]
    public async Task<IActionResult> Open(int number)
    {
        LightService.Controller.Write(LightService.Pins[number], PinValue.High);
        return Ok();
    }


    [HttpPost("{number}/close")]
    public async Task<IActionResult> Close(int number)
    {
        LightService.Controller.Write(LightService.Pins[number], PinValue.Low);
        return Ok();
    }
}
