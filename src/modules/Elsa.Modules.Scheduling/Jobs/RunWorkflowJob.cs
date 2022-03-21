using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.Models;

namespace Elsa.Modules.Scheduling.Jobs;

public class RunWorkflowJob : Job
{
    [JsonConstructor]
    public RunWorkflowJob()
    {
    }

    public RunWorkflowJob(string workflowId)
    {
        WorkflowId = workflowId;
    }

    public string WorkflowId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var request = new DispatchWorkflowDefinitionRequest(WorkflowId, 1);
        var workflowInvoker = context.GetRequiredService<IWorkflowInvoker>();
        await workflowInvoker.DispatchAsync(request, context.CancellationToken);
    }
}