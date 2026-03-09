using Elsa.Api.Client.Resources.Shells.Responses;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.Shells.Contracts;

[PublicAPI]
public interface IShellsApi
{
    [Post("/actions/shells/reload")]
    Task<IApiResponse<ShellReloadResponse>> ReloadAllAsync(CancellationToken cancellationToken = default);

    [Post("/shells/{shellId}/reload")]
    Task<IApiResponse<ShellReloadResponse>> ReloadAsync(string shellId, CancellationToken cancellationToken = default);
}