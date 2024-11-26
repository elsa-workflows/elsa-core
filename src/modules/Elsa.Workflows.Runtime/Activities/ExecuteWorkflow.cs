using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create a new workflow instance of the specified workflow and execute it.", Kind = ActivityKind.Task)]
[UsedImplicitly]
public class ExecuteWorkflow : Activity<ExecuteWorkflowResult>
{
    /// <inheritdoc />
    public ExecuteWorkflow([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The definition ID of the workflow to execute. 
    /// </summary>
    [Input(
        DisplayName = "Workflow Definition",
        Description = "The definition ID of the workflow to execute.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
    public Input<string> WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(
        DisplayName = "Correlation ID",
        Description = "The correlation ID to associate the workflow with."
    )]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// The input to send to the workflow.
    /// </summary>
    [Input(Description = "The input to send to the workflow.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = await ExecuteWorkflowAsync(context);
        context.SetResult(result);
        await context.CompleteActivityAsync();
    }

    private async ValueTask<ExecuteWorkflowResult> ExecuteWorkflowAsync(ActivityExecutionContext context)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();
        var correlationId = CorrelationId.GetOrDefault(context);
        var workflowRuntime = context.GetRequiredService<IWorkflowRuntime>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var workflowDefinitionService = context.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, VersionOptions.Published, context.CancellationToken);

        if (workflowGraph == null)
            throw new Exception($"No published version of workflow definition with ID {workflowDefinitionId} found.");

        var options = new StartWorkflowRuntimeParams
        {
            ParentWorkflowInstanceId = context.WorkflowExecutionContext.Id,
            Input = input,
            CorrelationId = correlationId,
            InstanceId = identityGenerator.GenerateId()
        };

        var workflowResult = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, options);
        var info = new ExecuteWorkflowResult
        {
            WorkflowInstanceId = workflowResult.WorkflowInstanceId,
            Status = workflowResult.Status,
            SubStatus = workflowResult.SubStatus,
            Output = workflowResult.Output
        };

        return info;
    }
}