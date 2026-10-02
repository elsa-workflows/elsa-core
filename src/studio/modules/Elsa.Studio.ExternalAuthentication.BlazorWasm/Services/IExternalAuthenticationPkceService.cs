using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Creates browser-crypto PKCE transactions for brokered authentication.</summary>
public interface IExternalAuthenticationPkceService
{
    Task<(ExternalAuthenticationPkceTransaction Transaction, string CodeChallenge)> CreateAsync(
        string? returnPath,
        CancellationToken cancellationToken = default);
}
