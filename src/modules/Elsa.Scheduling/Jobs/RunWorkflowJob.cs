using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Runtime.Models;
using Elsa.Jobs.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;

namespace Elsa.Scheduling.Jobs;

public class RunWorkflowJob : Job
{
    [JsonConstructor]
    public RunWorkflowJob()
    {
    }

    public RunWorkflowJob(string workflowId, IDictionary<string, object>? input = default, string? correlationId = default)
    {
        WorkflowId = workflowId;
        Input = input;
        CorrelationId = correlationId;
    }

    public string WorkflowId { get; set; } = default!;
    public IDictionary<string, object>? Input { get; set; }
    public string? CorrelationId { get; set; }

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var request = new DispatchWorkflowDefinitionRequest(WorkflowId, VersionOptions.Published, Input, CorrelationId);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}