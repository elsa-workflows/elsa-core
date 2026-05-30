using System.Net;
using System.Security.Cryptography;
using System.Text;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.AspNetCore.Http;

namespace Elsa.Diagnostics.OpenTelemetry.Ingestion;

public static class OtlpIngestionSecurity
{
    public static bool IsAuthorized(HttpContext httpContext, OpenTelemetryDiagnosticsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return options.AllowUnauthenticatedLoopback && IsLoopback(httpContext);

        return httpContext.Request.Headers.TryGetValue(options.ApiKeyHeaderName, out var value) && ApiKeysMatch(value.ToString(), options.ApiKey);
    }

    private static bool IsLoopback(HttpContext httpContext)
    {
        var remoteAddress = httpContext.Connection.RemoteIpAddress;
        return remoteAddress != null && IPAddress.IsLoopback(remoteAddress);
    }

    private static bool ApiKeysMatch(string providedApiKey, string expectedApiKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        var providedHash = SHA256.HashData(providedBytes);
        var expectedHash = SHA256.HashData(expectedBytes);

        try
        {
            return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(providedBytes);
            CryptographicOperations.ZeroMemory(expectedBytes);
            CryptographicOperations.ZeroMemory(providedHash);
            CryptographicOperations.ZeroMemory(expectedHash);
        }
    }
}
