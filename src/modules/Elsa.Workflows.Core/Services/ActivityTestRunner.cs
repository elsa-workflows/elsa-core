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
        var id = identityGenerator.GenerateId();
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, id, cancellationToken);
        var variableTestValues = GetVariableTestValues(workflowGraph);

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

    private IDictionary<string, object?> GetVariableTestValues(WorkflowGraph workflowGraph)
    {
        var variableTestValues = workflowGraph.Workflow.CustomProperties.TryGetValue("VariableTestValues", out var variableTestValuesObj) ? variableTestValuesObj : null;
        
        if(variableTestValues is null)
            return new Dictionary<string, object?>();

        if (variableTestValues is JsonElement jsonElement)
            variableTestValues = JsonSerializer.Deserialize<ExpandoObject>(jsonElement.GetRawText())!;

        var expandoObject = (ExpandoObject)variableTestValues;
        var parsedValues = new Dictionary<string, object?>();

        foreach (var (key, value) in expandoObject)
        {
            var variableDefinition = workflowGraph.Workflow.Variables.FirstOrDefault(x => x.Id == key);
            
            if (variableDefinition == null)
                continue;

            var variableType = variableDefinition.GetVariableType();
            var variableValue = value.ConvertTo(variableType);
            parsedValues[key] = variableValue;
        }
        
        return parsedValues;
    }
}