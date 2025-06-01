using System.Text.Json;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;
using static Elsa.Resilience.Endpoints.SimulateResponse.StatusCodeMessageLookup;

namespace Elsa.Resilience.Endpoints.SimulateResponse;

public class SimulateResponseEndpoint(IMemoryCache memoryCache) : EndpointWithoutRequest<SimulatedResponse>
{
    private static readonly TimeSpan SlidingExpirationTimeSpan = TimeSpan.FromMinutes(15);

    public override void Configure()
    {
        Get("/simulate-response");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sessionId = HttpContext.Request.Query["sessionId"].FirstOrDefault() ?? "default";
        var codes = GetCodes();
        var cacheKey = $"status-simulation-session-{sessionId}";
        var nextIndex = memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = SlidingExpirationTimeSpan;
            return 0;
        });

        var currentCode = nextIndex < codes.Length ? codes[nextIndex] : codes[^1];
        var message = StatusMessages.TryGetValue(currentCode, out var reason)
            ? reason
            : $"Status Code {currentCode}";

        if (nextIndex + 1 >= codes.Length)
        {
            memoryCache.Remove(cacheKey);
        }
        else
        {
            memoryCache.Set(cacheKey, nextIndex + 1, new MemoryCacheEntryOptions
            {
                SlidingExpiration = SlidingExpirationTimeSpan
            });
        }

        await SendAsync(new(message), currentCode, ct);
    }

    private int[] GetCodes()
    {
        var codesParam = HttpContext.Request.Query["codes"].FirstOrDefault();
        int[] defaultCodes = [429, 503, 200];
        return string.IsNullOrWhiteSpace(codesParam) ? defaultCodes : JsonSerializer.Deserialize<int[]>(codesParam)!;
    }
}