using GallagherCardholders.Services;
using Microsoft.AspNetCore.Mvc;

namespace GallagherCardholders.Controllers;

[ApiController]
[Route("api")]
public class CardholdersController : ControllerBase
{
    private readonly GallagherClient _client;

    public CardholdersController(GallagherClient client)
    {
        _client = client;
    }

    [HttpGet("cardholders")]
    public async Task<IActionResult> GetCardholders([FromQuery] string? search, [FromQuery] int? limit, [FromQuery] int? offset)
    {
        try
        {
            var result = await _client.GetCardholdersAsync(search, limit, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log detailed error to console
            Console.WriteLine($"[CardholdersController] Exception: {ex}");
            return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name });
        }
    }
}
