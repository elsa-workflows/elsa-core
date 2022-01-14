using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Abstractions;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;

namespace Elsa.Activities.Scheduling.Jobs;

public record RunWorkflowJob(string WorkflowId) : IJob
{
    public string JobId => $"workflow:{WorkflowId}";
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
        var request = new ExecuteWorkflowDefinitionRequest(job.WorkflowId, 1);
        await _workflowInvoker.ExecuteAsync(request, cancellationToken);
    }
}