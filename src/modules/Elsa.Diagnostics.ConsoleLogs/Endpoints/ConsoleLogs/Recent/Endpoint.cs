using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

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

        using var body = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(body, cancellationToken);

        if (body.Length == 0)
            return null;

        body.Position = 0;
        return await JsonSerializer.DeserializeAsync<ElsaConsoleLogFilter>(body, JsonSerializerOptions, cancellationToken);
    }
}
