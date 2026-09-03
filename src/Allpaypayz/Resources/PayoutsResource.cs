namespace Allpaypayz.Resources;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class PayoutsResource
{
    private readonly AllpaypayzHttpClient _http;
    internal PayoutsResource(AllpaypayzHttpClient http) { _http = http; }

    public async Task<Dictionary<string, object?>> CreateAsync(object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, "/v4/payouts", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> GetAsync(string id, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Get, $"/v4/payouts/{WebUtility.UrlEncode(id)}", null, null, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> FindByReferenceAsync(string merchantReference, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["merchant_reference"] = merchantReference };
        var env = await _http.RequestAsync(HttpMethod.Get, "/v4/payouts", null, query, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }
}
