using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using Humanizer;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<IActivity>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IActivityFactory _activityFactory;
    private readonly IActivityDescriber _activityDescriber;
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public ActivityJsonConverter(
        IActivityRegistry activityRegistry, 
        IActivityFactory activityFactory, 
        IActivityDescriber activityDescriber,
        IExpressionSyntaxRegistry expressionSyntaxRegistry,
        IServiceProvider serviceProvider)
    {
        _activityRegistry = activityRegistry;
        _activityFactory = activityFactory;
        _activityDescriber = activityDescriber;
        _expressionSyntaxRegistry = expressionSyntaxRegistry;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("type", out var activityTypeNameElement))
            throw new JsonException("Failed to extract activity type property");

        var activityTypeName = activityTypeNameElement.GetString()!;
        var activityTypeVersion = doc.RootElement.TryGetProperty("version", out var activityTypeVersionElement) ? activityTypeVersionElement.GetInt32() : 1;
        var activityDescriptor = _activityRegistry.Find(activityTypeName, activityTypeVersion);

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));

        if (activityDescriptor == null)
        {
            var notFoundContext = new ActivityConstructorContext(doc.RootElement, newOptions);
            var notFoundActivity = (NotFoundActivity)_activityFactory.Create(typeof(NotFoundActivity), notFoundContext);

            notFoundActivity.Type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>();
            notFoundActivity.MissingTypeName = activityTypeName;
            notFoundActivity.MissingTypeVersion = activityTypeVersion;
            return notFoundActivity;
        }

        var context = new ActivityConstructorContext(doc.RootElement, newOptions);
        var activity = activityDescriptor.Constructor(context);
        
        // Reconstruct inputs.
        foreach (var inputDefinition in activityDescriptor.Inputs)
        {
            var inputName = inputDefinition.Name;
            var propertyName = inputName.Camelize();
            var nakedType = inputDefinition.Type;
            var wrappedType = typeof(Input<>).MakeGenericType(nakedType);
                    
            if (doc.RootElement.TryGetProperty(propertyName, out var propertyElement) && propertyElement.ValueKind != JsonValueKind.Null && propertyElement.ValueKind != JsonValueKind.Undefined)
            {
                var json = propertyElement.ToString();
                var inputValue = JsonSerializer.Deserialize(json, wrappedType, newOptions);

                activity.SyntheticProperties[inputName] = inputValue!;
            }
        }

        return activity;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        var activityDescriptor = _activityRegistry.Find(value.Type, value.Version)!;
        var newOptions = new JsonSerializerOptions(options);
        
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));
        
        // Write to a JsonObject so that we can add additional information.
        var activityModel = JsonSerializer.SerializeToNode(value, value.GetType(), newOptions)!;
        var activityType = value.GetType();
        var typedInputs = _activityDescriber.DescribeInputProperties(activityType).ToDictionary(x => x.Name);
        var syntheticInputs = activityDescriptor.Inputs.Where(x => !typedInputs.ContainsKey(x.Name)).ToList(); 

        // Write synthetic inputs. 
        foreach (var inputDescriptor in syntheticInputs)
        {
            var inputName = inputDescriptor.Name;
            var propertyName = inputName.Camelize();
            
            if (value.SyntheticProperties.TryGetValue(inputName, out var inputValue))
            {
                var input = (Input?)inputValue;

                if (input == null)
                {
                    activityModel[propertyName] = null;
                    continue;
                }
                
                var expression = input.Expression;
                var expressionType = expression.GetType();
                var inputType = input.Type;
                var memoryReferenceId = input.MemoryBlockReference().Id;
                var expressionSyntaxDescriptor = _expressionSyntaxRegistry.Find(x => x.Type == expressionType);

                if (expressionSyntaxDescriptor == null)
                    throw new Exception($"Syntax descriptor with expression type {expressionType} not found in registry");

                var inputModel = new
                {
                    TypeName = inputType,
                    Expression = expressionSyntaxDescriptor.CreateSerializableObject(new SerializableObjectConstructorContext(expression)),
                    MemoryReference = new
                    {
                        Id = memoryReferenceId
                    }
                };

                activityModel[propertyName] = JsonSerializer.SerializeToNode(inputModel, inputModel.GetType(), newOptions);
            }
        }
        
        // Send the model to the writer.
        JsonSerializer.Serialize(writer, activityModel, newOptions);
    }
}