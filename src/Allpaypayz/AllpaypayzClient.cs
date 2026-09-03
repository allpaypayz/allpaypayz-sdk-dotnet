namespace Allpaypayz;

using System;
using Allpaypayz.Resources;

/// <summary>
/// Top-level Allpaypayz API v4 client.
/// </summary>
public sealed class AllpaypayzClient : IDisposable
{
    public const string SdkVersion = "0.1.0";
    private const string DefaultBaseUrl = "https://api4.allpaypayz.com";
    private const string BaseUserAgent = "Allpaypayz-SDK-DotNet/" + SdkVersion;

    public PaymentsResource Payments { get; }
    public PayoutsResource Payouts { get; }
    public P2PTransfersResource P2P { get; }
    public OrdersResource Orders { get; }
    public TerminalResource Terminal { get; }

    private readonly System.Net.Http.HttpClient? _ownedHttpClient;

    public AllpaypayzClient(
        string apiKey,
        string baseUrl = DefaultBaseUrl,
        string? apiVersion = null,
        RetryOptions? retry = null,
        string? userAgent = null,
        TimeSpan? requestTimeout = null,
        System.Net.Http.HttpClient? httpClient = null
    )
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Allpaypayz: api key is required", nameof(apiKey));
        }
        var ua = userAgent != null ? $"{BaseUserAgent} {userAgent}" : BaseUserAgent;
        var owned = httpClient == null;
        var http = httpClient ?? new System.Net.Http.HttpClient { Timeout = requestTimeout ?? TimeSpan.FromSeconds(30) };
        _ownedHttpClient = owned ? http : null;
        var inner = new AllpaypayzHttpClient(apiKey, baseUrl, ua, apiVersion, retry ?? RetryOptions.Defaults(), requestTimeout ?? TimeSpan.FromSeconds(30), http);
        Payments = new PaymentsResource(inner);
        Payouts = new PayoutsResource(inner);
        P2P = new P2PTransfersResource(inner);
        Orders = new OrdersResource(inner);
        Terminal = new TerminalResource(inner);
    }

    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
