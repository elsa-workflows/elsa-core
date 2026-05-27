using ConsoleLogStreaming.Core;
using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

[PublicAPI]
internal class Endpoint(IConsoleLogProvider provider) : ElsaEndpoint<ElsaConsoleLogFilter, RecentConsoleLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.POST);
        Routes("/diagnostics/console-logs/recent");
        ConfigurePermissions(ConsoleLogsPermissions.Read);
    }

    public override async Task<RecentConsoleLogsResult> ExecuteAsync(ElsaConsoleLogFilter request, CancellationToken cancellationToken)
    {
        return await provider.GetRecentAsync(ConsoleLogFilterMapper.ToStreamingFilter(request), cancellationToken);
    }
}
