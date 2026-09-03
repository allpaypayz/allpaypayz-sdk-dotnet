namespace Allpaypayz.Resources;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class PaymentsResource
{
    private readonly AllpaypayzHttpClient _http;
    internal PaymentsResource(AllpaypayzHttpClient http) { _http = http; }

    private static string Enc(string v) => WebUtility.UrlEncode(v);

    public async Task<Dictionary<string, object?>> CreateAsync(object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, "/v4/payments", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> CreateRedirectAsync(object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, "/v4/payments/redirect", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> RecurrentAsync(object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, "/v4/payments/recurrent", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> Finish3DSAsync(string id, object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, $"/v4/payments/{Enc(id)}/finish-3ds", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> GetAsync(string id, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Get, $"/v4/payments/{Enc(id)}", null, null, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> FindByReferenceAsync(string merchantReference, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["merchant_reference"] = merchantReference };
        var env = await _http.RequestAsync(HttpMethod.Get, "/v4/payments", null, query, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> CreateRefundAsync(string paymentId, object body, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Post, $"/v4/payments/{Enc(paymentId)}/refunds", body, null, idempotencyKey, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }

    public async Task<Dictionary<string, object?>> GetRefundAsync(string paymentId, string refundId, CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Get, $"/v4/payments/{Enc(paymentId)}/refunds/{Enc(refundId)}", null, null, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }
}
