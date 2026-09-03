namespace Allpaypayz;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Allpaypayz.Exceptions;

/// <summary>
/// Internal HTTP layer. Wraps <see cref="System.Net.Http.HttpClient"/> with
/// auth, auto-idempotency, retries with jitter, and v4 error mapping.
/// </summary>
internal sealed class AllpaypayzHttpClient
{
    private static readonly HashSet<int> RetryableStatuses = new() { 429, 500, 502, 503, 504 };
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly System.Net.Http.HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _userAgent;
    private readonly string? _apiVersion;
    private readonly RetryOptions _retry;
    private readonly Random _random = new();

    public AllpaypayzHttpClient(
        string apiKey,
        string baseUrl,
        string userAgent,
        string? apiVersion,
        RetryOptions retry,
        TimeSpan requestTimeout,
        System.Net.Http.HttpClient? httpClient
    )
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _userAgent = userAgent;
        _apiVersion = apiVersion;
        _retry = retry;
        _http = httpClient ?? new System.Net.Http.HttpClient { Timeout = requestTimeout };
    }

    public async Task<Dictionary<string, object?>> RequestAsync(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string?>? query = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default
    )
    {
        var uri = BuildUri(path, query);
        byte[]? bodyBytes = body is null ? null : JsonSerializer.SerializeToUtf8Bytes(body, JsonOpts);

        Exception? lastError = null;
        for (int attempt = 1; attempt <= _retry.MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.UserAgent.ParseAdd(_userAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_apiVersion != null)
            {
                request.Headers.Add("Accept-Api-Version", _apiVersion);
            }
            if (bodyBytes != null)
            {
                request.Content = new ByteArrayContent(bodyBytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            if (method == HttpMethod.Post)
            {
                request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < _retry.MaxAttempts)
                {
                    await SleepBackoffAsync(attempt, null, cancellationToken).ConfigureAwait(false);
                    lastError = e;
                    continue;
                }
                throw new AllpaypayzNetworkError("network", "network_error", e.Message, null, null, null, null);
            }

            using (response)
            {
                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                int status = (int)response.StatusCode;
                if (status < 400)
                {
                    return ParseJson(data);
                }

                int? retryAfter = ParseRetryAfter(response);
                Dictionary<string, object?>? payload = SafeJson(data);
                AllpaypayzException apiErr = BuildApiError(status, payload, retryAfter);

                if (RetryableStatuses.Contains(status) && attempt < _retry.MaxAttempts)
                {
                    await SleepBackoffAsync(attempt, retryAfter, cancellationToken).ConfigureAwait(false);
                    lastError = apiErr;
                    continue;
                }
                throw apiErr;
            }
        }
        throw lastError ?? new AllpaypayzException("api", "retry_exhausted", "all retries failed");
    }

    private string BuildUri(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var sb = new StringBuilder(_baseUrl);
        sb.Append(path.StartsWith('/') ? path : "/" + path);
        if (query == null || query.Count == 0)
        {
            return sb.ToString();
        }
        var qs = new StringBuilder();
        foreach (var kv in query)
        {
            if (kv.Value == null) continue;
            if (qs.Length > 0) qs.Append('&');
            qs.Append(WebUtility.UrlEncode(kv.Key));
            qs.Append('=');
            qs.Append(WebUtility.UrlEncode(kv.Value));
        }
        if (qs.Length > 0)
        {
            sb.Append('?');
            sb.Append(qs);
        }
        return sb.ToString();
    }

    private static Dictionary<string, object?> ParseJson(byte[] body)
    {
        if (body.Length == 0) return new Dictionary<string, object?>();
        try
        {
            using var doc = JsonDocument.Parse(body);
            return ElementToDict(doc.RootElement);
        }
        catch (JsonException e)
        {
            throw new AllpaypayzException("api", "invalid_json_response", e.Message);
        }
    }

    private static Dictionary<string, object?>? SafeJson(byte[] body)
    {
        if (body.Length == 0) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return ElementToDict(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, object?> ElementToDict(JsonElement el)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in el.EnumerateObject())
        {
            dict[prop.Name] = ConvertElement(prop.Value);
        }
        return dict;
    }

    private static object? ConvertElement(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object: return ElementToDict(el);
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in el.EnumerateArray()) list.Add(ConvertElement(item));
                return list;
            case JsonValueKind.String: return el.GetString();
            case JsonValueKind.Number:
                if (el.TryGetInt64(out var l)) return l;
                return el.GetDouble();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            default: return null;
        }
    }

    private static int? ParseRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter == null) return null;
        if (response.Headers.RetryAfter.Delta.HasValue)
        {
            return (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
        }
        if (response.Headers.RetryAfter.Date.HasValue)
        {
            int seconds = (int)(response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
            return seconds < 0 ? 0 : seconds;
        }
        return null;
    }

    private static AllpaypayzException BuildApiError(int status, Dictionary<string, object?>? payload, int? retryAfter)
    {
        Dictionary<string, object?>? err = null;
        if (payload != null && payload.TryGetValue("error", out var errObj) && errObj is Dictionary<string, object?> errDict)
        {
            err = errDict;
        }
        string type = err != null && err.TryGetValue("type", out var t) && t is string ts ? ts : StatusToType(status);
        string code = err != null && err.TryGetValue("code", out var c) && c is string cs ? cs : $"http_{status}";
        string message = err != null && err.TryGetValue("message", out var m) && m is string ms ? ms : $"Request failed with status {status}";

        string? requestId = payload != null && payload.TryGetValue("request_id", out var rid) && rid is string rids ? rids : null;

        IReadOnlyList<Dictionary<string, object?>>? details = null;
        if (err != null && err.TryGetValue("details", out var d) && d is List<object?> detailList)
        {
            var typed = new List<Dictionary<string, object?>>();
            foreach (var item in detailList)
            {
                if (item is Dictionary<string, object?> dict) typed.Add(dict);
            }
            details = typed;
        }

        return type switch
        {
            "validation"      => new AllpaypayzValidationError(type, code, message, status, requestId, details, retryAfter),
            "authentication"  => new AllpaypayzAuthenticationError(type, code, message, status, requestId, details, retryAfter),
            "not_found"       => new AllpaypayzNotFoundError(type, code, message, status, requestId, details, retryAfter),
            "conflict"        => new AllpaypayzConflictError(type, code, message, status, requestId, details, retryAfter),
            "business"        => new AllpaypayzBusinessError(type, code, message, status, requestId, details, retryAfter),
            "rate_limit"      => new AllpaypayzRateLimitError(type, code, message, status, requestId, details, retryAfter),
            "gateway"         => new AllpaypayzGatewayError(type, code, message, status, requestId, details, retryAfter),
            _                 => new AllpaypayzException(type, code, message, status, requestId, details, retryAfter),
        };
    }

    private static string StatusToType(int status) => status switch
    {
        400 => "validation",
        401 or 403 => "authentication",
        404 => "not_found",
        409 => "conflict",
        422 => "business",
        429 => "rate_limit",
        >= 500 and <= 599 => "gateway",
        _ => "api",
    };

    private async Task SleepBackoffAsync(int attempt, int? retryAfter, CancellationToken ct)
    {
        if (retryAfter.HasValue)
        {
            await Task.Delay(TimeSpan.FromSeconds(retryAfter.Value), ct).ConfigureAwait(false);
            return;
        }
        var initial = _retry.EffectiveInitialBackoff;
        var max = _retry.EffectiveMaxBackoff;
        var jitter = _retry.EffectiveJitter;
        var exp = TimeSpan.FromMilliseconds(Math.Min(max.TotalMilliseconds, initial.TotalMilliseconds * Math.Pow(2, attempt - 1)));
        var jitterValue = TimeSpan.FromMilliseconds(_random.NextDouble() * jitter.TotalMilliseconds);
        await Task.Delay(exp + jitterValue, ct).ConfigureAwait(false);
    }
}
