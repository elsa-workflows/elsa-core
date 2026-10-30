namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

/// <summary>Browser-local, one-time state retained between broker initiation and completion.</summary>
public sealed record ExternalAuthenticationPkceTransaction(
    string State,
    string CodeVerifier,
    string ReturnPath,
    DateTimeOffset ExpiresAt);
