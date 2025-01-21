using System.Text.Json;
using Elsa.CommandExecuter.Providers;
using Elsa.CommandExecuter.Utils;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Elsa.Extensions;
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
        var commandType = TypeUtils.GetType(Command.Get(context));
        var commandProperties = commandType.GetProperties();
        var constructorsPotentialValues = new Dictionary<string, object?>(/*context.WorkflowInstance.Variables.Data*/);
        //if (!constructorsPotentialValues.ContainsKey(WorkflowKeys.WorkflowInstanceId))
        //    constructorsPotentialValues.Add(WorkflowKeys.WorkflowInstanceId, context.WorkflowInstance.Id);
        //else
        //    constructorsPotentialValues[WorkflowKeys.WorkflowInstanceId] = context.WorkflowInstance.Id;

        //if (!string.IsNullOrWhiteSpace(NextStatus))
        //{
        //    if (!constructorsPotentialValues.ContainsKey(WorkflowKeys.NextStatus))
        //        constructorsPotentialValues.Add(WorkflowKeys.NextStatus, WorkflowEnumsProvider.Instance.ParseStatus(NextStatus));
        //    else
        //        constructorsPotentialValues[WorkflowKeys.NextStatus] = WorkflowEnumsProvider.Instance.ParseStatus(NextStatus);
        //}
        if (Payload != null)
        {
            var payloadItems = JsonSerializer.Deserialize<Dictionary<string, object>>(Payload.Get(context));
            AddRange(constructorsPotentialValues, payloadItems);
        }
        // create an instance of that type
        var commandInstance = Activator.CreateInstance(commandType);
        foreach (var prop in commandProperties)
        {
            var variable = constructorsPotentialValues.FirstOrDefault(x => x.Key.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
            object paramValue = null;
            if (variable.Key != null)
                if (variable.Value is System.Text.Json.JsonElement)
                {
                    if (prop.PropertyType == typeof(DateOnly))
                        prop.SetValue(commandInstance, ChangeType(DateOnly.Parse(variable.Value.ToString()), prop.PropertyType), null);
                    else if (prop.PropertyType == typeof(TimeOnly))
                        prop.SetValue(commandInstance, ChangeType(TimeOnly.ParseExact(variable.Value.ToString(), "HH:mm"), prop.PropertyType), null);
                    else
                    {
                        if (((System.Text.Json.JsonElement)variable.Value).ValueKind.ToString().ToLower() != "Undefined".ToLower())
                        {
                            prop.SetValue(commandInstance, ChangeType(
                           Newtonsoft.Json.JsonConvert.DeserializeObject
                           (((System.Text.Json.JsonElement)variable.Value).GetRawText(), prop.PropertyType), prop.PropertyType), null);
                        }
                        else
                        {
                            object defaultValue = prop.GetType().IsValueType ? Activator.CreateInstance(prop.GetType()) : null;
                            prop.SetValue(commandInstance, defaultValue);
                        }
                    }
                }
                else
                    prop.SetValue(commandInstance, ChangeType(variable.Value, prop.PropertyType), null);

        }
        _mediator = context.GetRequiredService<IMediator>();
        await _mediator.SendAsync((ICommand)commandInstance);

        await context.CompleteActivityAsync();


    }
    public object ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t);
    }
    private void AddRange(IDictionary<string, object> destination, Dictionary<string, object> source)
    {
        if (source != default && source.Count() > 0)
        {
            foreach (var item in source)
            {
                if (destination.ContainsKey(item.Key.ToLower()))
                    destination[item.Key.ToLower()] = item.Value;
                else
                    destination.Add(item.Key.ToLower(), item.Value);
            }
        }
    }

}
