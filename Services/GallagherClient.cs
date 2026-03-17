using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GallagherCardholders.Services;

public class GallagherClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _apiKey;
    private readonly string? _clientCertThumbprint;
    private readonly bool _ignoreServerCertificateErrors;
    private readonly HttpClient _http;

    public GallagherClient(IConfiguration config)
    {
        _host = config["Gallagher:Host"] ?? throw new ArgumentNullException("Gallagher:Host");
        _port = int.Parse(config["Gallagher:Port"] ?? "8443");
        _apiKey = config["Gallagher:ApiKey"] ?? throw new ArgumentNullException("Gallagher:ApiKey");
        _clientCertThumbprint = config["Gallagher:ClientCertificateThumbprint"];
        // Fix: parse string to bool to avoid CS0029
        _ignoreServerCertificateErrors = bool.Parse(config["Gallagher:IgnoreServerCertificateErrors"] ?? "false");

        var handler = new HttpClientHandler();

        // Ignorar errores de certificado del servidor si se pide
        if (_ignoreServerCertificateErrors)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        // Certificado cliente desde Windows Store (CurrentUser\My)
        if (!string.IsNullOrEmpty(_clientCertThumbprint))
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, _clientCertThumbprint, false);
            if (certs.Count == 0)
                throw new Exception($"Certificado no encontrado en store: {_clientCertThumbprint}");
            handler.ClientCertificates.Add(certs[0]);
            store.Close();
        }

        _http = new HttpClient(handler);
        _http.BaseAddress = new Uri($"https://{_host}:{_port}/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GGL-API-KEY", _apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<JsonElement> GetCardholdersAsync(string? search = null, int? limit = null, int? offset = null)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search!;
            if (limit.HasValue) query["limit"] = limit.Value.ToString();
            if (offset.HasValue) query["offset"] = offset.Value.ToString();
            var queryString = query.ToString();
            var url = string.IsNullOrWhiteSpace(queryString) ? "api/cardholders" : $"api/cardholders?{queryString}";
            Console.WriteLine($"[GallagherClient] GET {url}");
            var response = await _http.GetAsync(url);
            Console.WriteLine($"[GallagherClient] Status: {(int)response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            return document.RootElement.Clone();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GallagherClient] Exception: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
