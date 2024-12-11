using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using System.Text.Json.Serialization;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Maps the values from an input event (data present on the body of the request
/// as input parameters when initially invoking a workflow) to a set of variables.
///
/// For example the request to execute a workflow my include parameters like
/// "enterpriseId" or "salesforceId" and those fields on the request need to be
/// mapped to variable path names as the service registry often understands them
/// such as "cms-enterprise~1:enterprise:enterprise-id".
/// </summary>
[Activity(
    "Trimble.Elsa.Activities.Activities",
    "ServiceRegistry",
    "Projects workflow input data onto variables based on JSON Path.",
    DisplayName = "Input to Variable Mapper",
    Kind = ActivityKind.Task)]
public class InputEventMapper : CodeActivity
{
    /// <summary>
    /// Do not use for construction. Exists only to support serialization.
    /// </summary>
    [JsonConstructor]
    public InputEventMapper() {}

    /// <summary>
    /// Primary constructor.
    /// </summary>
    public InputEventMapper(ResponseMapRecord outputVariableMap)
    {
        OutputVariableMap = outputVariableMap;
    }

    /// <summary>
    /// Maps the JsonPath of the field in the body to the destination variable.
    /// </summary>
    public ResponseMapRecord OutputVariableMap { get; set; }
        = new("default", "default");


    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        if (context.WorkflowExecutionContext.Input is null)
        {
            context.LogError<InputEventMapper>("No input data was provided to the workflow.");
            return;
        }

        var inputValue = context.WorkflowExecutionContext.Input[OutputVariableMap.KeyAsFieldName]?.ToString();
        if (!context.WorkflowExecutionContext.Input.ContainsKey(OutputVariableMap.KeyAsFieldName)
            || string.IsNullOrEmpty(inputValue))
        {
            context.LogCritical<InputEventMapper>(
                $"An input value of [{OutputVariableMap.KeyAsFieldName}] was expected but was not provided when executing the workflow.",
                new { OutputVariableMap.KeyAsFieldName });
            return;
        }

        context.LogInfo<InputEventMapper>(
            $"Mapping input value",
            new
            {
                InputValue   = inputValue,
                VariableName = OutputVariableMap.VariableName
            });

        context.SetVariable(OutputVariableMap.VariableName, inputValue);
    }
}
