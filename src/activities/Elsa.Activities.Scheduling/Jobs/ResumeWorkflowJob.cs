using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Scheduling.Abstractions;
using Elsa.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Jobs;

public record ResumeWorkflowJob(string WorkflowInstanceId, string ActivityId) : IJob
{
    public string JobId => $"workflow-instance:{WorkflowInstanceId}-{ActivityId}";
}

public class ResumeWorkflowJobHandler : JobHandler<ResumeWorkflowJob>
{
    private readonly IWorkflowInvoker _workflowInvoker;
    public ResumeWorkflowJobHandler(IWorkflowInvoker workflowInvoker) => _workflowInvoker = workflowInvoker;

    protected override async Task HandleAsync(ResumeWorkflowJob job, CancellationToken cancellationToken)
    {
        var request = new ExecuteWorkflowInstanceRequest(job.WorkflowInstanceId);
        await _workflowInvoker.ExecuteAsync(request, cancellationToken);
    }
}