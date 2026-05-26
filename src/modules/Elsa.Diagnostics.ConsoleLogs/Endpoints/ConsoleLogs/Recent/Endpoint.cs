using ConsoleLogStreaming.Core;
using Elsa.Abstractions;
using Elsa.Diagnostics.ConsoleLogs.Permissions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using JetBrains.Annotations;
using ApiConsoleLogFilter = ConsoleLogStreaming.Contracts.ConsoleLogFilter;

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
        var result = await provider.GetRecentAsync(mapper.ToCore(ToApiFilter(request)), cancellationToken);
        return mapper.ToApi(result);
    }

    private static ApiConsoleLogFilter ToApiFilter(ConsoleLogFilter request)
    {
        var metadata = request.Metadata != null
            ? new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(request.WorkflowInstanceId))
            metadata[ConsoleLogMetadataKeys.WorkflowInstanceId] = request.WorkflowInstanceId;

        return new()
        {
            SourceId = request.SourceId,
            Stream = request.Stream,
            Query = request.Query,
            Metadata = metadata,
            From = request.From,
            To = request.To,
            Limit = request.Limit
        };
    }
}
