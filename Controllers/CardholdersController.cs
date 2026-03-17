using GallagherCardholders.Services;
using Microsoft.AspNetCore.Mvc;

namespace GallagherCardholders.Controllers;

[ApiController]
[Route("api")]
public class CardholdersController : ControllerBase
{
    private readonly GallagherClient _client;
    private readonly ImportService _importService;

    public CardholdersController(GallagherClient client, ImportService importService)
    {
        _client = client;
        _importService = importService;
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
            Console.WriteLine($"[CardholdersController] Exception: {ex}");
            return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [HttpPost("import")]
public async Task<IActionResult> ImportCardholders(IFormFile csvFile, [FromForm] string? mappingJson, CancellationToken cancellationToken)
{
    try
    {
        if (csvFile == null || csvFile.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        Dictionary<string, string> mapping = string.IsNullOrWhiteSpace(mappingJson)
            ? new Dictionary<string, string>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson)
              ?? new Dictionary<string, string>();

        var result = await _importService.ImportFromCsvAsync(csvFile.OpenReadStream(), mapping, cancellationToken);

        return Ok(new
        {
            result.Imported,
            result.Failed,
            result.Total,
            result.Errors
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CardholdersController] Import error: {ex}");
        return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name });
    }
}

    [HttpPost("cardholders")]
    public async Task<IActionResult> CreateSingleCardholder([FromBody] object cardholderData)
    {
        try
        {
            await _client.CreateCardholderAsync(cardholderData);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

