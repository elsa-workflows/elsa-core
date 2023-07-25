using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.Report;

/// <summary>
/// Gets a report for the specified set of activities that includes number of executions, split by completed and non-completed executions as well as blocking activities.
/// </summary>
[PublicAPI]
internal class Report : ElsaEndpoint<Request, Response>
{
    private readonly IActivityExecutionStore _store;
    private readonly IActivityExecutionService _activityExecutionService;

    /// <inheritdoc />
    public Report(IActivityExecutionStore store, IActivityExecutionService activityExecutionService)
    {
        _store = store;
        _activityExecutionService = activityExecutionService;
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
        var stats = (await _activityExecutionService.GetStatsAsync(request.WorkflowInstanceId, request.ActivityIds, cancellationToken)).ToList();
        
        return new Response
        {
            Stats = stats
        };
    }
}