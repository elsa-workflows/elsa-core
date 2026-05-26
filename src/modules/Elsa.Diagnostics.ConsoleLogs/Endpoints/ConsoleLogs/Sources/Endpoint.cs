using ConsoleLogStream.Core;
using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Sources;

[PublicAPI]
internal class Endpoint(IConsoleLogProvider provider) : ElsaEndpointWithoutRequest<IReadOnlyCollection<ConsoleLogSource>>
{
    public override void Configure()
    {
        Get("/diagnostics/console-logs/sources");
        ConfigurePermissions(ConsoleLogsPermissions.Read);
    }

    public override async Task<IReadOnlyCollection<ConsoleLogSource>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await provider.ListSourcesAsync(cancellationToken);
    }
}
