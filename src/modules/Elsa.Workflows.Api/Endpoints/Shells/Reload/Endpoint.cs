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
        var serializerOptions = apiSerializer.GetOptions();

        if (result.Status == ShellReloadStatus.NotFound)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var response = Elsa.Workflows.Api.Endpoints.Shells.Reload.Response.FromResult(result);
        var statusCode = result.Status switch
        {
            ShellReloadStatus.Busy => StatusCodes.Status409Conflict,
            ShellReloadStatus.RequestedShellFailed => StatusCodes.Status422UnprocessableEntity,
            ShellReloadStatus.Failed => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status200OK
        };

        await SendJsonAsync(response, statusCode, serializerOptions, cancellationToken);
    }

    private async Task SendJsonAsync(Response response, int statusCode, System.Text.Json.JsonSerializerOptions serializerOptions, CancellationToken cancellationToken)
    {
        HttpContext.Response.StatusCode = statusCode;
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}
