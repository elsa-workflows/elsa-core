using ConsoleLogStreaming.Core;
using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

[PublicAPI]
internal class Endpoint(IConsoleLogProvider provider) : ElsaEndpointWithoutRequest<RecentConsoleLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.POST);
        Routes("/diagnostics/console-logs/recent");
        ConfigurePermissions(ConsoleLogsPermissions.Read);
    }

    public override async Task<RecentConsoleLogsResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var request = await ReadJsonBodyAsync(cancellationToken) ?? new();
        return await provider.GetRecentAsync(ConsoleLogFilterMapper.ToStreamingFilter(request), cancellationToken);
    }

    private async Task<ElsaConsoleLogFilter?> ReadJsonBodyAsync(CancellationToken cancellationToken)
    {
        if (HttpContext.Request.ContentLength is 0)
            return null;

        if (!HttpContext.Request.HasJsonContentType())
            return null;

        return await HttpContext.Request.ReadFromJsonAsync<ElsaConsoleLogFilter>(cancellationToken);
    }
}
