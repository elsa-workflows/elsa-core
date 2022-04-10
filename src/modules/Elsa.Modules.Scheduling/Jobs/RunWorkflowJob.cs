using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Jobs.Models;
using Elsa.Persistence.Models;

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
        var request = new DispatchWorkflowDefinitionRequest(WorkflowId, VersionOptions.Published);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}