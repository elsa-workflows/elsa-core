using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

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