using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityTestRunner(
    IServiceProvider serviceProvider,
    IWorkflowExecutionPipeline pipeline,
    IIdentityGenerator identityGenerator)
    : IActivityTestRunner
{
    /// <inheritdoc />
    public async Task<ActivityExecutionContext> RunAsync(WorkflowGraph workflowGraph, IActivity activity, CancellationToken cancellationToken = default)
    {
        // Tag the workflow as test execution so that activities can adjust their behavior if needed.


        var id = identityGenerator.GenerateId();
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, id, cancellationToken);
        workflowExecutionContext.TransientProperties[ActivityTestRunner.VariableTestValuesPropertyName] = true;
        var variableTestValues = workflowGraph.Workflow.GetTestVariables();

        foreach (var variable in workflowGraph.Workflow.Variables)
        {
            var variableValue = variableTestValues.TryGetValue(variable.Id, out var value) ? value : variable.Value;
            variable.Set(workflowExecutionContext.ExpressionExecutionContext!, variableValue);
        }

        workflowExecutionContext.ScheduleActivity(activity);
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);

        await pipeline.ExecuteAsync(workflowExecutionContext);
        var activityExecutionContext = workflowExecutionContext
            .ActivityExecutionContexts
            .First(x => x.Activity == activity);
        return activityExecutionContext;
    }
}