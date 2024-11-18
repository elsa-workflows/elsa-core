using System.ComponentModel;
using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Agents.Activities;

/// <summary>
/// An activity that executes a function of a skilled agent. This is an internal activity that is used by <see cref="AgentActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class AgentActivity : CodeActivity
{
    private static JsonSerializerOptions? _serializerOptions;

    private static JsonSerializerOptions SerializerOptions =>
        _serializerOptions ??= new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true
        }.WithConverters(new ExpandoObjectConverterFactory());

    [JsonIgnore] internal string AgentName { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;
        var functionInput = new Dictionary<string, object?>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : default;
            functionInput[inputDescriptor.Name] = inputValue;
        }

        var agentInvoker = context.GetRequiredService<AgentInvoker>();
        var result = await agentInvoker.InvokeAgentAsync(AgentName, functionInput, context.CancellationToken);
        var json = result.FunctionResult.GetValue<string>();
        var outputType = context.ActivityDescriptor.Outputs.Single().Type;

        // If the target type is object, we want the JSON to be deserialized into an ExpandoObject for dynamic field access. 
        if (outputType == typeof(object))
            outputType = typeof(ExpandoObject);

        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        var outputValue = json.ConvertTo(outputType, converterOptions);
        var outputDescriptor = activityDescriptor.Outputs.Single();
        var output = (Output)outputDescriptor.ValueGetter(this);
        context.Set(output, outputValue);
    }
}