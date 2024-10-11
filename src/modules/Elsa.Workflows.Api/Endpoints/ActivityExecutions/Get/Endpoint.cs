using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Get;

/// <summary>
/// Gets an individual execution for a given activity.
/// </summary>
[PublicAPI]
internal class Endpoint(IActivityExecutionStore store) : ElsaEndpointWithoutRequest<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/activity-executions/{id}");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<string>("id");
        var filter = new ActivityExecutionRecordFilter
        {
            Id = id
        };

        var record = await store.FindAsync(filter, cancellationToken);

        if (record == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        await SendOkAsync(record, cancellationToken);
    }
}