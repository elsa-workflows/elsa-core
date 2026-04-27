using CShells.Lifecycle;
using Elsa.Abstractions;
using Elsa.Shells.Api.Endpoints.Shells;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Shells.Api.Endpoints.Shells.Reload;

[PublicAPI]
internal class Reload(IShellRegistry shellRegistry, IApiSerializer apiSerializer) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/{shellId}/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var shellId = Route<string>("shellId")!;
        var result = await shellRegistry.ReloadAsync(shellId, cancellationToken);

        // CShells 0.0.15 surfaces failures via ReloadResult.Error rather than throwing — translate to 404
        // (which historically signalled "blueprint not found") for any composition / initialization error.
        if (result.Error is not null)
        {
            var failedResponse = new ShellReloadResponse
            {
                Status = ShellReloadStatus.NotFound,
                RequestedShellId = shellId,
                Timestamp = DateTimeOffset.UtcNow,
                Message = result.Error.Message
            };
            await SendResponseAsync(failedResponse, StatusCodes.Status404NotFound, cancellationToken);
            return;
        }

        var response = new ShellReloadResponse
        {
            Status = ShellReloadStatus.Completed,
            RequestedShellId = shellId,
            Timestamp = DateTimeOffset.UtcNow
        };
        await SendResponseAsync(response, StatusCodes.Status200OK, cancellationToken);
    }

    private async Task SendResponseAsync(ShellReloadResponse response, int statusCode, CancellationToken cancellationToken)
    {
        var serializerOptions = apiSerializer.GetOptions();
        HttpContext.Response.StatusCode = statusCode;
        await HttpContext.Response.WriteAsJsonAsync(response, serializerOptions, cancellationToken);
    }
}
