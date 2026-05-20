using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Resilience.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using static Elsa.Resilience.Endpoints.SimulateResponse.StatusCodeMessageLookup;

namespace Elsa.Resilience.Endpoints.SimulateResponse;

public class SimulateResponseEndpoint(SimulateResponseSessionStore sessionStore, IOptions<SimulateResponseOptions> options) : ElsaEndpointWithoutRequest<SimulatedResponse>
{
    private static readonly int[] DefaultCodes = [429, 503, 200];
    private readonly SimulateResponseOptions _options = options.Value;

    public override void Configure()
    {
        Get("/simulate-response");
        ConfigurePermissions("exec:*", "exec:resilience", "exec:resilience:simulate-response");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!TryGetSessionId(out var sessionId, out var error) || !TryGetCodes(out var codes, out error))
        {
            AddError(error);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        var scopedSessionId = CreateScopedSessionId(sessionId);
        if (!sessionStore.TryGetNextIndex(scopedSessionId, codes.Length, out var nextIndex))
        {
            AddError($"The maximum number of active simulate-response sessions ({_options.SessionCapacity}) has been reached.");
            await Send.ErrorsAsync(StatusCodes.Status429TooManyRequests, ct);
            return;
        }

        var currentCode = codes[nextIndex];
        var message = StatusMessages.TryGetValue(currentCode, out var reason)
            ? reason
            : $"Status Code {currentCode}";

        await Send.ResponseAsync(new(message), currentCode, ct);
    }

    private bool TryGetSessionId(out string sessionId, out string error)
    {
        sessionId = "default";
        error = "";
        var sessionIdParam = HttpContext.Request.Query["sessionId"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sessionIdParam))
            return true;

        if (sessionIdParam.Length > _options.MaxSessionIdLength)
        {
            error = $"The sessionId query parameter must be {_options.MaxSessionIdLength} characters or fewer.";
            return false;
        }

        sessionId = sessionIdParam;
        return true;
    }

    private bool TryGetCodes(out int[] codes, out string error)
    {
        codes = DefaultCodes;
        error = "";
        var codesParam = HttpContext.Request.Query["codes"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(codesParam))
            return true;

        if (codesParam.Length > _options.MaxCodesQueryLength)
        {
            error = $"The codes query parameter must be {_options.MaxCodesQueryLength} characters or fewer.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(codesParam);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The codes query parameter must be a JSON array of HTTP status codes.";
                return false;
            }

            var parsedCodes = new List<int>();
            foreach (var codeElement in document.RootElement.EnumerateArray())
            {
                if (parsedCodes.Count >= _options.MaxCodes)
                {
                    error = $"The codes query parameter can contain at most {_options.MaxCodes} status codes.";
                    return false;
                }

                if (codeElement.ValueKind != JsonValueKind.Number || !codeElement.TryGetInt32(out var code) || code is < 100 or > 599)
                {
                    error = "The codes query parameter must contain HTTP status codes between 100 and 599.";
                    return false;
                }

                parsedCodes.Add(code);
            }

            if (parsedCodes.Count == 0)
            {
                error = "The codes query parameter must contain at least one status code.";
                return false;
            }

            codes = parsedCodes.ToArray();
            return true;
        }
        catch (JsonException)
        {
            error = "The codes query parameter must be valid JSON.";
            return false;
        }
    }

    private string CreateScopedSessionId(string sessionId)
    {
        var key = $"{GetAuthenticatedIdentityKey()}\0{sessionId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    private string GetAuthenticatedIdentityKey()
    {
        var user = HttpContext.User;
        var identityClaim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? user.FindFirst("sub")
            ?? user.FindFirst("client_id")
            ?? user.FindFirst("name")
            ?? user.FindFirst(ClaimTypes.Name);

        if (identityClaim != null)
            return $"{identityClaim.Type}:{identityClaim.Value}";

        if (!string.IsNullOrWhiteSpace(user.Identity?.Name))
            return $"name:{user.Identity.Name}";

        var claimsKey = string.Join('\u001e', user.Claims
            .Where(x => x.Type != "permissions")
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Issuer, StringComparer.Ordinal)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .Select(x => $"{x.Type}:{x.Issuer}:{x.Value}"));

        return !string.IsNullOrEmpty(claimsKey)
            ? claimsKey
            : $"auth:{user.Identity?.AuthenticationType ?? "unknown"}";
    }
}
