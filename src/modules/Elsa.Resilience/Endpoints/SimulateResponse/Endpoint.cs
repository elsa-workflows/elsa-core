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

        if (!sessionStore.TryGetNextIndex(sessionId, codes.Length, out var nextIndex))
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
}
