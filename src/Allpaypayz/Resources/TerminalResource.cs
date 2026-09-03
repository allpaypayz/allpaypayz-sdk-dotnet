namespace Allpaypayz.Resources;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class TerminalResource
{
    private readonly AllpaypayzHttpClient _http;
    internal TerminalResource(AllpaypayzHttpClient http) { _http = http; }

    public async Task<Dictionary<string, object?>> GetAsync(CancellationToken ct = default)
    {
        var env = await _http.RequestAsync(HttpMethod.Get, "/v4/terminal", null, null, null, ct).ConfigureAwait(false);
        return (Dictionary<string, object?>)env["data"]!;
    }
}
