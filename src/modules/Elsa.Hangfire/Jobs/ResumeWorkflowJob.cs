using Elsa.Common.Multitenancy;
using Elsa.Scheduling;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(IWorkflowRuntime workflowRuntime, ITenantFinder tenantFinder, ITenantAccessor tenantAccessor)
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="request">The workflow request.</param>
    /// <param name="tenantId">The ID of the current tenant scheduling this job.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(ScheduleExistingWorkflowInstanceRequest request, string? tenantId, CancellationToken cancellationToken)
    {
        var tenant = tenantId != null ? await tenantFinder.FindByIdAsync(tenantId, cancellationToken) : null;
        using var scope = tenantAccessor.PushContext(tenant);
        var client = await workflowRuntime.CreateClientAsync(request.WorkflowInstanceId, cancellationToken);
        var runRequest = new RunWorkflowInstanceRequest
        {
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            Input = request.Input,
            Properties = request.Properties
        };
        await client.RunInstanceAsync(runRequest, cancellationToken);
    }
}
