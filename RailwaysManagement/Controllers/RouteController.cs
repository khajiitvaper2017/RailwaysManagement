using Microsoft.AspNetCore.Mvc;
using RailwaysManagement.Services;

namespace RailwaysManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteController(OpenRailRoutingService router) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> Get(string from, string to)
    {
        var coords = router.FindRouteAsync(from, to).Result;
        return Task.FromResult<IActionResult>(Ok(new { path = coords }));
    }
}