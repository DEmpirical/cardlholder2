using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;

namespace GallagherCardholders.Services;

public class ImportService
{
    private readonly GallagherClient _client;
    private readonly ILogger<ImportService> _logger;
    private readonly int _batchSize;
    private readonly int _maxDegreeOfParallelism;

    public ImportService(GallagherClient client, ILogger<ImportService> logger, int batchSize = 25, int maxDegreeOfParallelism = 4)
    {
        _client = client;
        _logger = logger;
        _batchSize = batchSize;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public async Task<ImportResult> ImportFromCsvAsync(Stream csvStream, Dictionary<string, string> mapping, CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        var errors = new List<ImportError>();
        var records = new List<Dictionary<string, string>>();

        // Parse CSV with CsvHelper
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null, // Ignore missing fields
            HeaderValidated = null,   // Ignore header validation
            Delimiter = ","
        });

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        // Validate mapping against headers (case-insensitive)
        var normalizedHeaders = headers.Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var normalizedMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in mapping)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            // Find header index (case-insensitive)
            var idx = Array.FindIndex(normalizedHeaders, h => h.Equals(key.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                normalizedMapping[headers[idx]] = value; // Use original header name for index
            }
            else
            {
                _logger.LogWarning($"Mapping column '{key}' not found in CSV header. Available: {string.Join(", ", headers)}");
            }
        }

        // Read all records
        while (await csv.ReadAsync())
        {
            if (cancellationToken.IsCancellationRequested) break;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header)?.Trim() ?? string.Empty;
            }
            records.Add(row);
        }

        result.Total = records.Count;

        // Process in batches
        var batches = records.Chunk(_batchSize);
        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism);
            var tasks = batch.Select(async (record, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var cardholder = MapCardholder(record, normalizedMapping);
                    var validation = ValidateCardholder(cardholder);
                    if (!validation.IsValid)
                    {
                        errors.Add(new ImportError
                        {
                            Row = result.Processed + index + 2, // +2 because header + 1-based
                            Error = validation.ErrorMessage ?? "Validation failed"
                        });
                        return;
                    }

                    await _client.CreateCardholderAsync(cardholder);
                    Interlocked.Increment(ref result.Imported);
                }
                catch (Exception ex)
                {
                    errors.Add(new ImportError
                    {
                        Row = result.Processed + index + 2,
                        Error = ex.Message
                    });
                    _logger.LogError(ex, "Failed to import row {Row}", result.Processed + index + 2);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            result.Processed += batch.Length;
        }

        result.Failed = errors.Count;
        result.Errors = errors.Take(10).ToList();

        return result;
    }

    private Dictionary<string, object> MapCardholder(Dictionary<string, string> row, Dictionary<string, string> mapping)
    {
        var cardholder = new Dictionary<string, object>();

        foreach (var kvp in mapping)
        {
            var csvHeader = kvp.Key;
            var apiField = kvp.Value;

            if (row.TryGetValue(csvHeader, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                cardholder[apiField] = value.Trim();
            }
        }

        // Ensure required fields
        if (!cardholder.ContainsKey("firstName") && row.TryGetValue("firstName", out var fn) && !string.IsNullOrWhiteSpace(fn))
            cardholder["firstName"] = fn.Trim();
        if (!cardholder.ContainsKey("lastName") && row.TryGetValue("lastName", out var ln) && !string.IsNullOrWhiteSpace(ln))
            cardholder["lastName"] = ln.Trim();

        return cardholder;
    }

    private (bool IsValid, string? ErrorMessage) ValidateCardholder(Dictionary<string, object> cardholder)
    {
        if (!cardholder.ContainsKey("firstName") || string.IsNullOrWhiteSpace(cardholder["firstName"]?.ToString()))
            return (false, "Missing required field: firstName");
        if (!cardholder.ContainsKey("lastName") || string.IsNullOrWhiteSpace(cardholder["lastName"]?.ToString()))
            return (false, "Missing required field: lastName");

        return (true, null);
    }
}

public class ImportResult
{
    public int Total { get; set; }
    public int Imported { get; set; }
    public int Failed { get; set; }
    public int Processed { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}

public class ImportError
{
    public int Row { get; set; }
    public string Error { get; set; } = string.Empty;
}
