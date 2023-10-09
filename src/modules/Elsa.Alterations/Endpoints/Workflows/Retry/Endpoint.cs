using Elsa.Abstractions;
using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Results;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Workflows.Retry;

/// <summary>
/// Retries the specified workflow instances.
/// </summary>
[PublicAPI]
public class Retry : ElsaEndpoint<Request, Response>
{
    private readonly IAlterationRunner _alterationRunner;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    /// <inheritdoc />
    public Retry(IAlterationRunner alterationRunner, IWorkflowDispatcher workflowDispatcher, IWorkflowInstanceStore workflowInstanceStore)
    {
        _alterationRunner = alterationRunner;
        _workflowDispatcher = workflowDispatcher;
        _workflowInstanceStore = workflowInstanceStore;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/alterations/workflows/retry");
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        ConfigurePermissions("run:alterations");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var allResults = new List<RunAlterationsResult>();
        
        // Load each workflow instance.
        var workflowInstances = (await _workflowInstanceStore.FindManyAsync(new WorkflowInstanceFilter { Ids = request.WorkflowInstanceIds }, cancellationToken)).ToList();

        foreach (var workflowInstance in workflowInstances)
        {
            // Setup an alteration plan.
            var activityIds = GetActivityIds(request, workflowInstance).ToList();
            var alterations = activityIds.Select(activityId => new ScheduleActivity { ActivityId = activityId }).Cast<IAlteration>().ToList();
            
            // Run the plan.
            var results = await _alterationRunner.RunAsync(request.WorkflowInstanceIds, alterations, cancellationToken);
            allResults.AddRange(results);
            
            // Schedule updated workflow.
            await _workflowDispatcher.DispatchAsync(new DispatchWorkflowInstanceRequest(workflowInstance.Id), cancellationToken);
        }
        
        // Write response.
        var response = new Response(allResults);
        await SendOkAsync(response, cancellationToken);
    }

    private IEnumerable<string> GetActivityIds(Request request, WorkflowInstance workflowInstance)
    {
        // If activity IDs are explicitly specified, use them.
        if (request.ActivityIds?.Any() == true)
            return request.ActivityIds;

        // Otherwise, select IDs of all faulted activities.
        return workflowInstance.WorkflowState.Incidents.Select(x => x.ActivityId).Distinct().ToList();
    }
}