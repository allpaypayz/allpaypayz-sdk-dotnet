namespace Allpaypayz.Resources;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class P2PTransfersResource
{
    private readonly AllpaypayzHttpClient _http;
    internal P2PTransfersResource(AllpaypayzHttpClient http) { _http = http; }

    public async Task<Dictionary<string, object?>> CreateAsync(object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, "/v4/p2p-transfers", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> ConfirmAsync(string id, object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, $"/v4/p2p-transfers/{WebUtility.UrlEncode(id)}/confirm", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> GetAsync(string id, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Get, $"/v4/p2p-transfers/{WebUtility.UrlEncode(id)}", null, null, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> FindByReferenceAsync(string merchantReference, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["merchant_reference"] = merchantReference };
        var env = await _http.RequestAsync(HttpMethod.Get, "/v4/p2p-transfers", null, query, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }
}
