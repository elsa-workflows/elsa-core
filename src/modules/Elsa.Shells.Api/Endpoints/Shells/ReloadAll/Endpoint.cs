using CShells.Lifecycle;
using Elsa.Abstractions;
using Elsa.Shells.Api.Endpoints.Shells;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elsa.Shells.Api.Endpoints.Shells.ReloadAll;

[PublicAPI]
internal class ReloadAll(IShellRegistry shellRegistry, IApiSerializer apiSerializer, ILogger<ReloadAll> logger) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/shells/reload");
        ConfigurePermissions("actions:shells:reload");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // CShells 0.0.15: ReloadActiveAsync replaces the old ReloadAllShellsAsync. Per-shell failures are surfaced
        // in each ReloadResult.Error rather than thrown; aggregate any errors into a single 503 response.
        var results = await shellRegistry.ReloadActiveAsync(cancellationToken: cancellationToken);
        var errors = results.Where(r => r.Error is not null).ToList();

        if (errors.Count > 0)
        {
            var summary = string.Join("; ", errors.Select(r => $"{r.Name}: {r.Error!.Message}"));
            logger.LogError("Failed to reload {Count} shell(s): {Summary}", errors.Count, summary);
            var failedResponse = new ShellReloadResponse
            {
                Status = ShellReloadStatus.Failed,
                Timestamp = DateTimeOffset.UtcNow,
                Message = summary
            };
            await SendResponseAsync(failedResponse, StatusCodes.Status503ServiceUnavailable, cancellationToken);
            return;
        }

        var response = new ShellReloadResponse
        {
            Status = ShellReloadStatus.Completed,
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