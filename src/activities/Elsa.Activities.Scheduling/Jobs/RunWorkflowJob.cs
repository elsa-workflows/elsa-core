using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Jobs;

public class RunWorkflowJob : IJob
{
    public RunWorkflowJob()
    {
    }

    public RunWorkflowJob(string workflowId)
    {
        WorkflowId = workflowId;
    }

    public string JobId => $"workflow:{WorkflowId}";
    public string WorkflowId { get; init; } = default!;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}