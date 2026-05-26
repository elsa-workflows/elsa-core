using ConsoleLogStreaming.Core;
using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

[PublicAPI]
internal class Endpoint(IConsoleLogProvider provider, IConsoleLogStreamingApiMapper mapper) : ElsaEndpoint<ConsoleLogFilter, RecentConsoleLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.POST);
        Routes("/diagnostics/console-logs/recent");
        ConfigurePermissions(ConsoleLogsPermissions.Read);
    }

    public override async Task<RecentConsoleLogsResult> ExecuteAsync(ConsoleLogFilter request, CancellationToken cancellationToken)
    {
        var result = await provider.GetRecentAsync(mapper.ToCore(request), cancellationToken);
        return mapper.ToApi(result);
    }
}
