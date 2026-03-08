using Elsa.Abstractions;
using Elsa.Workflows.Api.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Elsa.Workflows;

namespace Elsa.Workflows.Api.Endpoints.Shells.Reload;

[PublicAPI]
internal class Reload(IShellReloadOrchestrator shellReloadOrchestrator, IApiSerializer apiSerializer) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/{shellId}/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var shellId = Route<string>("shellId")!;
        var result = await shellReloadOrchestrator.ReloadAsync(shellId, cancellationToken);
        var response = Elsa.Workflows.Api.Endpoints.Shells.Reload.Response.FromResult(result);
        var serializerOptions = apiSerializer.GetOptions();

        switch (result.Status)
        {
            case ShellReloadStatus.NotFound:
                await Send.NotFoundAsync(cancellationToken);
                break;
            case ShellReloadStatus.Busy:
                await SendJsonAsync(response, StatusCodes.Status409Conflict, serializerOptions, cancellationToken);
                break;
            case ShellReloadStatus.RequestedShellFailed:
                await SendJsonAsync(response, StatusCodes.Status422UnprocessableEntity, serializerOptions, cancellationToken);
                break;
            case ShellReloadStatus.Failed:
                await SendJsonAsync(response, StatusCodes.Status503ServiceUnavailable, serializerOptions, cancellationToken);
                break;
            default:
                await SendJsonAsync(response, StatusCodes.Status200OK, serializerOptions, cancellationToken);
                break;
        }
    }

    private async Task SendJsonAsync(Response response, int statusCode, System.Text.Json.JsonSerializerOptions serializerOptions, CancellationToken cancellationToken)
    {
        HttpContext.Response.StatusCode = statusCode;
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}