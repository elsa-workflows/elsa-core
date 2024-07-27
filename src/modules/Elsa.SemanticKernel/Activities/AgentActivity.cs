using System.ComponentModel;
using Elsa.Expressions.Helpers;
using Elsa.SemanticKernel.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Newtonsoft.Json;

namespace Elsa.SemanticKernel.Activities;

/// <summary>
/// An activity that executes a function of a skilled agent. This is an internal activity that is used by <see cref="AgentActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class AgentActivity : CodeActivity
{
    [JsonIgnore] internal Agent Agent { get; set; } = default!;
    [JsonIgnore] internal string Skill { get; set; } = default!;
    [JsonIgnore] internal string Function { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;
        var functionInput = new Dictionary<string, object?>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input)inputDescriptor.ValueGetter(this)!;
            var inputValue = context.Get(input.MemoryBlockReference());
            functionInput[inputDescriptor.Name] = inputValue;
        }
        
        var result = await Agent.ExecuteAsync(Skill, Function, functionInput, context.CancellationToken);
        var json = result.FunctionResult.GetValue<string>();
        var outputType = context.ActivityDescriptor.Outputs.Single().Type;
        var outputValue = json.ConvertTo(outputType);

        var outputDescriptor = activityDescriptor.Outputs.Single();
        var output = (Output)outputDescriptor.ValueGetter(this);
        context.Set(output, outputValue);
    }
}