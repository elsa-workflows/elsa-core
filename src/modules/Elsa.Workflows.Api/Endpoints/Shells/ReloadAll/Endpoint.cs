using Elsa.Abstractions;
using Elsa.Workflows.Api.Contracts;
using Elsa.Workflows.Api.Endpoints.Shells;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.Shells.ReloadAll;

[PublicAPI]
internal class ReloadAll(IShellReloadOrchestrator shellReloadOrchestrator) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/actions/shells/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await shellReloadOrchestrator.ReloadAllAsync(cancellationToken);
        var response = ShellReloadResponse.FromResult(result);

        switch (result.Status)
        {
            case ShellReloadStatus.Busy:
                await Send.ResponseAsync(response, StatusCodes.Status409Conflict, cancellationToken);
                break;
            case ShellReloadStatus.Failed:
                await Send.ResponseAsync(response, StatusCodes.Status503ServiceUnavailable, cancellationToken);
                break;
            default:
                await Send.OkAsync(response, cancellationToken);
                break;
        }
    }
}