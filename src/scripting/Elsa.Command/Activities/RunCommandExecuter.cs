using System.Text.Json;
using Elsa.CommandExecuter.Providers;
using Elsa.CommandExecuter.Utils;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Elsa.Extensions;
using Elsa.CommandExecuter.Contracts;
namespace Elsa.Command.Activities;

[Activity("Elsa", "Scripting", "Runs a command predefined from the code base", DisplayName = "Run C# Command")]
public class RunCommandExecuter : Activity
{
    private IMediator _mediator;

    [Input(UIHandler = typeof(WorkflowCommandProvider), UIHint = InputUIHints.DropDown, Description = "Command to be executed")]
    public Input<string> Command
    {
        get;
        set;
    }

    [Input(UIHint = InputUIHints.MultiLine, Description = "Requested Body")]
    public Input<string> Payload
    {
        get;
        set;
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var constructorsPotentialValues = new Dictionary<string, object?>();
        constructorsPotentialValues.Add(nameof(WorkFlowCommand.WorkflowIntanceId), context.WorkflowExecutionContext.ParentWorkflowInstanceId);
        constructorsPotentialValues.Add(nameof(WorkFlowCommand.WorkflowCorrelationId), context.WorkflowExecutionContext.CorrelationId);
        constructorsPotentialValues.Add(nameof(WorkFlowCommand.WorkflowContextId), context.WorkflowExecutionContext.Id);

        if (Payload != null)
        {
            var payloadItems = JsonSerializer.Deserialize<Dictionary<string, object>>(Payload.Get(context));
            TypeUtils.AddRangeOrOverwrite(constructorsPotentialValues, payloadItems);
        }
        var commandType = TypeUtils.GetType(Command.Get(context));
        var commandProperties = commandType.GetProperties();
 
        var commandInstance = Activator.CreateInstance(commandType);
        foreach (var prop in commandProperties)
        {
            var variable = constructorsPotentialValues.FirstOrDefault(x => x.Key.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (variable.Key == null)
                continue;

            object paramValue = null;

            if (variable.Value is System.Text.Json.JsonElement jsonElement)
            {
                paramValue = HandleJsonElementValue(jsonElement, prop.PropertyType);
            }
            else
            {
                paramValue = TypeUtils.ChangeType(variable.Value, prop.PropertyType);
            }

            prop.SetValue(commandInstance, paramValue, null);
        }

        _mediator = context.GetRequiredService<IMediator>();
        await _mediator.SendAsync((ICommand)commandInstance);
        await context.CompleteActivityAsync();
    }


    // in case of having a complex object
    private static object HandleJsonElementValue(System.Text.Json.JsonElement jsonElement, Type propertyType)
    {
        if (propertyType == typeof(DateOnly))
        {
            return TypeUtils.ChangeType(DateOnly.Parse(jsonElement.ToString()), propertyType);
        }

        if (propertyType == typeof(TimeOnly))
        {
            return TypeUtils.ChangeType(TimeOnly.ParseExact(jsonElement.ToString(), "HH:mm"), propertyType);
        }

        if (jsonElement.ValueKind.ToString().Equals("Undefined", StringComparison.OrdinalIgnoreCase))
        {
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }

        return TypeUtils.ChangeType(
            Newtonsoft.Json.JsonConvert.DeserializeObject(jsonElement.GetRawText(), propertyType),
            propertyType
        );
    }

}
