using System.ComponentModel;
using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using static Elsa.Agents.Activities.Extensions.ResponseHelpers;

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

    [JsonIgnore] internal string AgentName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;
        var functionInput = new Dictionary<string, object?>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;

            if (inputValue is ExpandoObject expandoObject)
                inputValue = expandoObject.ConvertTo<string>();
            
            functionInput[inputDescriptor.Name] = inputValue;
        }

        var agentInvoker = context.GetRequiredService<IAgentInvoker>();
        var request = new InvokeAgentRequest
        {
            AgentName = AgentName,
            Input = functionInput,
            CancellationToken = context.CancellationToken
        };
        var agentExecutionResponse = await agentInvoker.InvokeAsync(request);
        var responseText = StripCodeFences(agentExecutionResponse.ChatMessageContent.Content!);
        var isJsonResponse = IsJsonResponse(responseText);
        var outputType = context.ActivityDescriptor.Outputs.Single().Type;

        // If the target type is object and the response is in JSON format, we want it to be deserialized into an ExpandoObject for dynamic field access. 
        if (outputType == typeof(object) && isJsonResponse)
            outputType = typeof(ExpandoObject);

        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        var outputValue = isJsonResponse ? responseText.ConvertTo(outputType, converterOptions) : responseText;
        var outputDescriptor = activityDescriptor.Outputs.Single();
        var output = (Output?)outputDescriptor.ValueGetter(this);
        context.Set(output, outputValue, "Output");
    }
}