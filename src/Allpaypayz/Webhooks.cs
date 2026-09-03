namespace Allpaypayz;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Allpaypayz.Exceptions;

public static class Webhooks
{
    private static readonly Regex SignatureRegex =
        new(@"^t=(\d+),v1=([0-9a-fA-F]+)$", RegexOptions.Compiled);

    /// <summary>
    /// Verify a Allpaypayz webhook delivery. Returns the parsed event field on
    /// success; throws <see cref="AllpaypayzWebhookError"/> with a
    /// machine-readable Code on any failure mode.
    /// </summary>
    public static Dictionary<string, object?> Verify(
        byte[] rawBody,
        string signatureHeader,
        string signKey,
        int toleranceSeconds = 300,
        DateTimeOffset? now = null
    )
    {
        var match = SignatureRegex.Match(signatureHeader?.Trim() ?? string.Empty);
        if (!match.Success)
        {
            throw new AllpaypayzWebhookError("invalid_signature_header",
                $"Malformed Callback-Signature: {signatureHeader}");
        }
        long ts = long.Parse(match.Groups[1].Value);
        string providedHex = match.Groups[2].Value.ToLowerInvariant();
        byte[] provided;
        try { provided = HexToBytes(providedHex); }
        catch (System.FormatException e)
        {
            throw new AllpaypayzWebhookError("invalid_signature_header", e.Message);
        }

        byte[] tsPrefix = Encoding.UTF8.GetBytes(ts + ".");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signKey));
        hmac.TransformBlock(tsPrefix, 0, tsPrefix.Length, null, 0);
        hmac.TransformFinalBlock(rawBody ?? System.Array.Empty<byte>(), 0, rawBody?.Length ?? 0);
        byte[] expected = hmac.Hash!;

        if (!CryptographicOperations.FixedTimeEquals(provided, expected))
        {
            throw new AllpaypayzWebhookError("signature_mismatch", "Webhook signature does not match");
        }

        long currentUnix = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        if (System.Math.Abs(currentUnix - ts) > toleranceSeconds)
        {
            throw new AllpaypayzWebhookError("stale_delivery",
                $"Webhook timestamp {ts} outside {toleranceSeconds}s tolerance (now={currentUnix})");
        }

        if (rawBody == null || rawBody.Length == 0)
        {
            throw new AllpaypayzWebhookError("invalid_envelope", "Webhook body is empty");
        }
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawBody);
        }
        catch (JsonException e)
        {
            throw new AllpaypayzWebhookError("invalid_json", $"Webhook body is not valid JSON: {e.Message}");
        }
        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("event", out var eventEl) ||
                eventEl.ValueKind != JsonValueKind.Object)
            {
                throw new AllpaypayzWebhookError("invalid_envelope", "Webhook envelope missing event field");
            }
            var eventDict = ElementToDict(eventEl);
            if (!eventDict.ContainsKey("type"))
            {
                throw new AllpaypayzWebhookError("invalid_envelope", "Webhook event missing type field");
            }
            return eventDict;
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

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new System.FormatException("odd-length hex");
        }
        byte[] result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return result;
    }
}

public sealed class WebhookDispatcher
{
    private readonly Dictionary<string, System.Action<Dictionary<string, object?>>> _handlers = new();

    public WebhookDispatcher On(string eventType, System.Action<Dictionary<string, object?>> handler)
    {
        _handlers[eventType] = handler;
        return this;
    }

    public void Dispatch(Dictionary<string, object?> evt)
    {
        if (!evt.TryGetValue("type", out var t) || t is not string type) return;
        if (_handlers.TryGetValue(type, out var h))
        {
            h(evt);
        }
    }
}
