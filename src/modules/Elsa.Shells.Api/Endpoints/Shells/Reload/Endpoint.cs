using CShells.Management;
using Elsa.Abstractions;
using Elsa.Shells.Api.Endpoints.Shells;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Shells.Api.Endpoints.Shells.Reload;

[PublicAPI]
internal class Reload(IShellManager shellManager, IApiSerializer apiSerializer) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/{shellId}/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var shellId = Route<string>("shellId")!;

        try
        {
            await shellManager.ReloadShellAsync(shellId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Shell not found by the provider.
            var notFoundResponse = new ShellReloadResponse
            {
                Status = ShellReloadStatus.NotFound,
                RequestedShellId = shellId,
                ReloadedAt = DateTimeOffset.UtcNow
            };
            await SendResponseAsync(notFoundResponse, StatusCodes.Status404NotFound, cancellationToken);
            return;
        }

        var response = new ShellReloadResponse
        {
            Status = ShellReloadStatus.Completed,
            RequestedShellId = shellId,
            ReloadedAt = DateTimeOffset.UtcNow
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
