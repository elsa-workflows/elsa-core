using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Scheduling.Jobs;

public class RunWorkflowJob : IJob
{
    public RunWorkflowJob()
    {
    }
    
    public RunWorkflowJob(WorkflowIdentity workflowIdentity)
    {
        WorkflowIdentity = workflowIdentity;
    }

    public string JobId => $"workflow:{WorkflowIdentity.DefinitionId}";
    public WorkflowIdentity WorkflowIdentity { get; init; } = default!;
    

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}