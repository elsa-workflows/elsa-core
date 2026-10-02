using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Retains broker credentials according to the selected browser persistence policy.</summary>
public interface IExternalAuthenticationBrowserTokenStore
{
    Task<ExternalAuthenticationTokenSet?> GetAsync(CancellationToken cancellationToken = default);
    Task SetAsync(ExternalAuthenticationTokenSet tokens, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
