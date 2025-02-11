using Elsa.Common.Multitenancy;
using Elsa.Scheduling;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Hangfire;
using JetBrains.Annotations;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class RunWorkflowJob(IWorkflowRuntime workflowRuntime, ITenantFinder tenantFinder, ITenantAccessor tenantAccessor)
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="taskName">A unique name for this job.</param>
    /// <param name="request">The workflow request.</param>
    /// <param name="tenantId">The ID of the current tenant scheduling this job.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    // ReSharper disable once UnusedParameter.Global
    [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ExecuteAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string? tenantId, CancellationToken cancellationToken)
    {
        var tenant = tenantId != null ? await tenantFinder.FindByIdAsync(tenantId, cancellationToken) : null;
        using var scope = tenantAccessor.PushContext(tenant);
        var client = await workflowRuntime.CreateClientAsync(cancellationToken);
        var createAndRunRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            TriggerActivityId = request.TriggerActivityId,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            Properties = request.Properties,
            ParentId = request.ParentId
        };
        await client.CreateAndRunInstanceAsync(createAndRunRequest, cancellationToken);
    }
    
    [Obsolete("Use the other overload.")]
    [UsedImplicitly]
    public async Task ExecuteAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(cancellationToken);
        var createAndRunRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            TriggerActivityId = request.TriggerActivityId,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            Properties = request.Properties,
            ParentId = request.ParentId
        };
        await client.CreateAndRunInstanceAsync(createAndRunRequest, cancellationToken);
    }
}