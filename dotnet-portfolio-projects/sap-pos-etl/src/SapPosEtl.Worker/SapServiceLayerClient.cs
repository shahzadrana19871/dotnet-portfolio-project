using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SapPosEtl.Worker;

/// <summary>
/// Minimal SAP Business One Service Layer client. The session cookie (B1SESSION) is kept by the
/// HttpClientHandler's CookieContainer; on a 401 the client logs in again and retries once.
/// </summary>
public class SapServiceLayerClient(HttpClient http, IOptions<SapOptions> options) : ISapClient
{
    // The Service Layer expects PascalCase property names, so camelCase defaults are disabled.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _loginGate = new(1, 1);
    private bool _loggedIn;

    private sealed record CreatedOrder(int DocEntry);

    public async Task<int> CreateSalesOrderAsync(SalesOrderDto order, CancellationToken ct)
    {
        await EnsureLoggedInAsync(ct);

        var response = await http.PostAsJsonAsync("Orders", order, Json, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _loggedIn = false;
            await EnsureLoggedInAsync(ct);
            response = await http.PostAsJsonAsync("Orders", order, Json, ct);
        }

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedOrder>(cancellationToken: ct);
        return created?.DocEntry ?? throw new InvalidOperationException("SAP did not return a DocEntry.");
    }

    private async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (_loggedIn) return;

        await _loginGate.WaitAsync(ct);
        try
        {
            if (_loggedIn) return;

            var sap = options.Value;
            var response = await http.PostAsJsonAsync(
                "Login",
                new { CompanyDB = sap.CompanyDb, UserName = sap.UserName, Password = sap.Password },
                Json,
                ct);
            response.EnsureSuccessStatusCode();
            _loggedIn = true;
        }
        finally
        {
            _loginGate.Release();
        }
    }
}
