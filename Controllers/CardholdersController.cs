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

    [HttpPost("import")]
    public async Task<IActionResult> ImportCardholders(IFormFile csvFile, [FromForm] string mappingJson)
    {
        try
        {
            if (csvFile == null || csvFile.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var mapping = string.IsNullOrEmpty(mappingJson)
                ? new Dictionary<string, string>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);

            using var reader = new StreamReader(csvFile.OpenReadStream());
            var lines = await reader.ReadToEndAsync().Split('\n');
            if (lines.Length < 2)
                return BadRequest(new { error = "CSV empty or missing header" });

            var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            int imported = 0, failed = 0;
            var errors = new List<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var fields = line.Split(',').Select(f => f.Trim()).ToArray();
                var cardholder = new Dictionary<string, object>();

                foreach (var kvp in mapping)
                {
                    var colIndex = Array.IndexOf(headers, kvp.Key);
                    if (colIndex >= 0 && colIndex < fields.Length)
                    {
                        var val = fields[colIndex];
                        if (!string.IsNullOrEmpty(val))
                            cardholder[kvp.Value] = val;
                    }
                }

                // Campos requeridos
                if (!cardholder.ContainsKey("firstName") || !cardholder.ContainsKey("lastName"))
                {
                    failed++;
                    errors.Add($"Row {i+1}: missing required fields (firstName, lastName)");
                    continue;
                }

                try
                {
                    await _client.CreateCardholderAsync(cardholder);
                    imported++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Row {i+1}: {ex.Message}");
                }
            }

            return Ok(new
            {
                imported,
                failed,
                total = lines.Length - 1,
                errors = errors.Take(10).ToArray()
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
