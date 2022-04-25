using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Runtime.Models;
using Elsa.Jobs.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;

namespace Elsa.Modules.Scheduling.Jobs;

public class RunWorkflowJob : Job
{
    [JsonConstructor]
    public RunWorkflowJob()
    {
    }

    public RunWorkflowJob(string workflowId, string? correlationId = default)
    {
        WorkflowId = workflowId;
        CorrelationId = correlationId;
    }

    public string WorkflowId { get; set; } = default!;
    public string? CorrelationId { get; set; }

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var request = new DispatchWorkflowDefinitionRequest(WorkflowId, VersionOptions.Published, CorrelationId: CorrelationId);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}