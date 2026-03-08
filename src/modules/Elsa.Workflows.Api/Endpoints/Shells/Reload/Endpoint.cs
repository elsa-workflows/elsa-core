using Elsa.Abstractions;
using Elsa.Workflows.Api.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.Shells.Reload;

[PublicAPI]
internal class Reload(IShellReloadOrchestrator shellReloadOrchestrator) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Post("/shells/{shellId}/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await shellReloadOrchestrator.ReloadAsync(request.ShellId, cancellationToken);
        var response = Elsa.Workflows.Api.Endpoints.Shells.Reload.Response.FromResult(result);

        switch (result.Status)
        {
            case ShellReloadStatus.NotFound:
                await Send.NotFoundAsync(cancellationToken);
                break;
            case ShellReloadStatus.Busy:
                await Send.ResponseAsync(response, StatusCodes.Status409Conflict, cancellationToken);
                break;
            case ShellReloadStatus.RequestedShellFailed:
                await Send.ResponseAsync(response, StatusCodes.Status422UnprocessableEntity, cancellationToken);
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