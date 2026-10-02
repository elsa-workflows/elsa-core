using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Stores a short-lived, one-time PKCE transaction in browser tab storage.</summary>
public interface IExternalAuthenticationPkceTransactionStore
{
    Task SaveAsync(ExternalAuthenticationPkceTransaction transaction, CancellationToken cancellationToken = default);
    Task<ExternalAuthenticationPkceTransaction?> TakeAsync(string state, CancellationToken cancellationToken = default);
}
