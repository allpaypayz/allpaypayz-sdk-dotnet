# `Allpaypayz.Sdk` (.NET)

**[⬇ Download the latest version](https://github.com/allpaypayz/allpaypayz-sdk-dotnet/archive/refs/heads/main.zip)** · [Browse the code](https://github.com/allpaypayz/allpaypayz-sdk-dotnet) · [MIT](LICENSE)

<sub>The archive is a snapshot of `main` — the current state of the SDK. Tagged releases will appear on the Releases page once the code leaves alpha.</sub>


Official Allpaypayz API v4 SDK for .NET.

> Status: **alpha** (v0.1.0). Multi-targeted at `net6.0` and `net8.0`.

## Install

```bash
dotnet add package Allpaypayz.Sdk --version 0.1.0
```

Only runtime dependency: `System.Text.Json` (already in the BCL on
`net6.0+`). No HTTP client library — uses the platform
`System.Net.Http.HttpClient`.

## Quick start

```csharp
using Allpaypayz;

using var client = new AllpaypayzClient(apiKey: Environment.GetEnvironmentVariable("ALLPAYPAYZ_API_KEY"));

var payment = await client.Payments.CreateAsync(new
{
    merchant_reference = "ORDER-77",
    amount = new { amount_minor = 1000, currency = "USD" },
    description = "Order #77",
    customer = new { name = "Jane Doe", email = "jane@example.com" },
    card = new
    {
        pan = "4111111111111111",
        exp_month = 12, exp_year = 2029,
        cvc = "123", holder = "JANE DOE",
    },
});

if ((string?)payment["status"] == "requires_action")
{
    // Redirect customer through ((Dictionary<string, object?>)payment["three_ds"]!)["acs_url"]
}
```

The client auto-injects `Idempotency-Key` (`Guid.NewGuid().ToString()`) on
every POST. Override per call with the `idempotencyKey:` parameter.

## Configuration

```csharp
using var client = new AllpaypayzClient(
    apiKey: "sk_test_...",
    baseUrl: "https://staging-api4.allpaypayz.com",
    apiVersion: "2026-05-20",
    requestTimeout: TimeSpan.FromSeconds(30),
    retry: new RetryOptions(
        MaxAttempts: 3,
        InitialBackoff: TimeSpan.FromMilliseconds(250),
        MaxBackoff: TimeSpan.FromSeconds(4),
        Jitter: TimeSpan.FromMilliseconds(250)
    ),
    userAgent: "MyApp/2.0"
);
```

Cancellation works through `CancellationToken` on every async method.

Inject your own `System.Net.Http.HttpClient` via the `httpClient:` parameter
for connection pooling, mTLS, custom handlers, etc.

## Resources

| Resource | Methods |
|---|---|
| `client.Payments` | `CreateAsync`, `CreateRedirectAsync`, `RecurrentAsync`, `Finish3DSAsync`, `GetAsync`, `FindByReferenceAsync`, `CreateRefundAsync`, `GetRefundAsync` |
| `client.Payouts`  | `CreateAsync`, `GetAsync`, `FindByReferenceAsync` |
| `client.P2P`      | `CreateAsync`, `ConfirmAsync`, `GetAsync`, `FindByReferenceAsync` |
| `client.Orders`   | `CreateAsync`, `GetAsync`, `FindByReferenceAsync` |
| `client.Terminal` | `GetAsync` |

Methods accept any object that's JSON-serializable for the request body and
return `Dictionary<string, object?>` (the `data` field of the v4 envelope,
recursively converted to native CLR types — no `JsonElement` leakage).

## Errors

```csharp
using Allpaypayz.Exceptions;

try
{
    await client.Payments.CreateAsync(req);
}
catch (AllpaypayzConflictError e) when (e.Code == "duplicate_reference")
{
    // merchant_reference already used on this terminal
}
```

| HTTP / `error.type` | Class |
|---|---|
| `400` / `validation` | `AllpaypayzValidationError` |
| `401`, `403` / `authentication` | `AllpaypayzAuthenticationError` |
| `404` / `not_found` | `AllpaypayzNotFoundError` |
| `409` / `conflict` | `AllpaypayzConflictError` |
| `422` / `business` | `AllpaypayzBusinessError` |
| `429` / `rate_limit` | `AllpaypayzRateLimitError` (`.RetryAfterSeconds`) |
| `5xx` / `gateway` | `AllpaypayzGatewayError` |
| Network / transport | `AllpaypayzNetworkError` |

All in `Allpaypayz.Exceptions`. Each carries `ErrorType`, `Code`, `Status`,
`RequestId`, `Details`, `RetryAfterSeconds`.

## Webhooks

```csharp
using Allpaypayz;
using Allpaypayz.Exceptions;

var dispatcher = new WebhookDispatcher()
    .On("payment.succeeded", evt => MarkOrderPaid(((Dictionary<string, object?>)evt["resource"]!)["merchant_reference"] as string))
    .On("payment.failed",    evt => MarkOrderFailed(((Dictionary<string, object?>)evt["resource"]!)["merchant_reference"] as string));

// In your ASP.NET Core minimal-API endpoint:
app.MapPost("/webhooks/allpaypayz", async (HttpRequest req) =>
{
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);
    try
    {
        var evt = Webhooks.Verify(
            ms.ToArray(),
            req.Headers["Callback-Signature"].ToString(),
            Environment.GetEnvironmentVariable("ALLPAYPAYZ_SIGN_KEY")!
        );
        dispatcher.Dispatch(evt);
        return Results.Ok();
    }
    catch (AllpaypayzWebhookError e)
    {
        return Results.BadRequest(e.Code);
    }
});
```

`Webhooks.Verify` parses `Callback-Signature` (`t=<unix>,v1=<hex>`),
recomputes `HMAC-SHA256(t + "." + raw_body, signKey)` via
`System.Security.Cryptography.HMACSHA256` and runs
`CryptographicOperations.FixedTimeEquals` for constant-time comparison,
rejecting deliveries outside the 300 s tolerance window.

## Tests

```bash
dotnet test
```

`tests/Allpaypayz.Tests/WebhooksTests.cs` loads `../spec/test-vectors.json`
through a project-relative include and guarantees byte-identity with every
other Allpaypayz SDK. `ClientTests.cs` uses a queue-based
`HttpMessageHandler` stub instead of a third-party mock library.

## License

MIT
