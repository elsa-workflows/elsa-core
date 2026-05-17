using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

[PublicAPI]
internal class Endpoint(IConsoleLogProvider provider) : ElsaEndpoint<ConsoleLogFilter, RecentConsoleLogsResult>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/diagnostics/console-logs/recent");
        ConfigurePermissions(ConsoleLogsPermissions.Read);
    }

    public override async Task<RecentConsoleLogsResult> ExecuteAsync(ConsoleLogFilter request, CancellationToken cancellationToken)
    {
        return await provider.GetRecentAsync(request, cancellationToken);
    }
}
