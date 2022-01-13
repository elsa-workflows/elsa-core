using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Jobs;

public class ResumeWorkflowJob : IJob
{
    public ResumeWorkflowJob()
    {
    }

    public ResumeWorkflowJob(string workflowInstanceId, string activityId)
    {
        WorkflowInstanceId = workflowInstanceId;
        ActivityId = activityId;
    }

    public string JobId => $"workflow-instance:{WorkflowInstanceId}-{ActivityId}";
    public string WorkflowInstanceId { get; init; } = default!;
    public string ActivityId { get; init; } = default!;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}