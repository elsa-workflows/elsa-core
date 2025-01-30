using System.Reflection;
using System.Text.Json;
using Elsa.Connections.Attributes;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.UIHints.Dropdown;
using JetBrains.Annotations;

namespace Elsa.Connections.Middleware;

/// <summary>
/// Adds extension methods to <see cref="ExecutionLogMiddleware"/>.
/// </summary>
public static class ConnectionMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ConnectionMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseConnectionMiddleware(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ConnectionMiddleware>();
}

/// <summary>
/// An activity execution middleware component that extracts execution details as <see cref="WorkflowExecutionLogEntry"/> objects.
/// </summary>
[UsedImplicitly]
public class ConnectionMiddleware(ActivityMiddlewareDelegate next, IConnectionRepository connectionStore) : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;

        if (activityDescriptor.Attributes.Any(attr => attr.GetType() == typeof(ConnectionActivityAttribute)))
        {
            var inputDescriptors = activityDescriptor.Inputs.Where(x => x?.PropertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(ConnectionProperties<>));

            if (inputDescriptors != null && inputDescriptors.Any())
            {
                var input = inputDescriptors.First();
                var propertyType = input?.PropertyInfo?.PropertyType.GetGenericArguments()[0];

                dynamic inputValue = input.ValueGetter(context.Activity);

                var connectionName = (string)inputValue?.ConnectionName;

                //Get connection from store, if exist,
                var connectionConfiguration = await connectionStore.GetConnectionAsync(connectionName);
                if (connectionConfiguration != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    options.Converters.Add(new Int32Converter());

                    dynamic deserializedjson =
                        JsonSerializer.Deserialize(connectionConfiguration.ConnectionConfiguration, propertyType, options);

                    inputValue.Properties = deserializedjson;

                    input.ValueSetter(context.Activity, inputValue);
                }
            }
        }

        await next(context);
    }

    private static bool IsActivityBookmarked(ActivityExecutionContext context)
    {
        return context.WorkflowExecutionContext.Bookmarks.Any(b => b.ActivityNodeId.Equals(context.ActivityNode.NodeId));
    }
}

public class Int32Converter : System.Text.Json.Serialization.JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string stringValue = reader.GetString();
            if (int.TryParse(stringValue, out int value))
            {
                return value;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
