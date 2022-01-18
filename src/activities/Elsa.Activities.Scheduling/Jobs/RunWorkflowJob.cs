using System.Threading;
using System.Threading.Tasks;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Scheduling.Abstractions;
using Elsa.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Jobs;

public record RunWorkflowJob(string WorkflowId) : IJob
{
    public string JobId => WorkflowId;
}

public class RunWorkflowJobHandler : JobHandler<RunWorkflowJob>
{
    private readonly IWorkflowInvoker _workflowInvoker;

    public RunWorkflowJobHandler(IWorkflowInvoker workflowInvoker)
    {
        _workflowInvoker = workflowInvoker;
    }

    protected override async Task HandleAsync(RunWorkflowJob job, CancellationToken cancellationToken)
    {
        var request = new DispatchWorkflowDefinitionRequest(job.WorkflowId, 1);
        await _workflowInvoker.DispatchAsync(request, cancellationToken);
    }
}