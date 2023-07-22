using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Report;

/// <summary>
/// Gets a report for the specified set of activities that includes number of executions, split by completed and non-completed executions as well as blocking activities.
/// </summary>
[PublicAPI]
internal class Report : ElsaEndpoint<Request, Response>
{
    private readonly IActivityExecutionLogStore _store;

    /// <inheritdoc />
    public Report(IActivityExecutionLogStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/activity-executions/report");
        ConfigurePermissions("read:activity-execution");
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityIds = request.ActivityIds,
        };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var records = (await _store.FindManyAsync(filter, order, cancellationToken)).ToList();
        var groupedRecords = records.GroupBy(x => x.ActivityId).ToList();
        var stats = groupedRecords.Select(grouping => new ActivityExecutionStats
        {
            ActivityId = grouping.Key,
            StartedCount = grouping.Count(),
            CompletedCount = grouping.Count(x => x.CompletedAt != null),
            UncompletedCount = grouping.Count(x => x.CompletedAt == null),
            IsBlocked = grouping.Any(x => x.HasBookmarks)
        }).ToList();
        
        return new Response
        {
            Stats = stats
        };
    }
}