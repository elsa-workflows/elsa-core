using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<IActivity>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IActivityFactory _activityFactory;
    private readonly IExpressionDescriptorRegistry _expressionDescriptorRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActivityJsonConverter> _logger;

    /// <inheritdoc />
    public ActivityJsonConverter(
        IActivityRegistry activityRegistry,
        IActivityFactory activityFactory,
        IExpressionDescriptorRegistry expressionDescriptorRegistry,
        IServiceProvider serviceProvider,
        ILogger<ActivityJsonConverter> logger)
    {
        _activityRegistry = activityRegistry;
        _activityFactory = activityFactory;
        _expressionDescriptorRegistry = expressionDescriptorRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        var activityRoot = doc.RootElement;
        var activityTypeName = GetActivityDetails(activityRoot, out var activityTypeVersion, out var activityDescriptor);
        var notFoundActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>();

        // If the activity type is a NotFoundActivity, try to extract the original activity type name and version.
        if (activityTypeName.Equals(notFoundActivityTypeName) && activityRoot.TryGetProperty("originalActivityJson", out var originalActivityJson))
        {
            activityRoot = JsonDocument.Parse(originalActivityJson.GetString()!).RootElement;
            activityTypeName = GetActivityDetails(activityRoot, out activityTypeVersion, out activityDescriptor);
        }

        var clonedOptions = GetClonedOptions(options);

        // If the activity type is not found, create a NotFoundActivity instead.
        if (activityDescriptor == null)
        {
            var notFoundActivityDescriptor = _activityRegistry.Find<NotFoundActivity>()!;
            var notFoundContext = new ActivityConstructorContext(notFoundActivityDescriptor, activityRoot, clonedOptions);
            var notFoundActivity = (NotFoundActivity)_activityFactory.Create(typeof(NotFoundActivity), notFoundContext);

            notFoundActivity.Type = notFoundActivityTypeName;
            notFoundActivity.Version = 1;
            notFoundActivity.MissingTypeName = activityTypeName;
            notFoundActivity.MissingTypeVersion = activityTypeVersion;
            notFoundActivity.OriginalActivityJson = activityRoot.ToString();
            notFoundActivity.SetDisplayText($"Not Found: {activityTypeName}");
            notFoundActivity.SetDescription($"Could not find activity type {activityTypeName} with version {activityTypeVersion}");
            return notFoundActivity;
        }

        var context = new ActivityConstructorContext(activityDescriptor, activityRoot, clonedOptions);
        var activity = activityDescriptor.Constructor(context);
        return activity;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        var clonedOptions = GetClonedOptions(options);
        var activityDescriptor = _activityRegistry.Find(value.Type, value.Version);
        var ignoreChildActivities = options.Converters.Any(x => x is IgnoreChildActivitiesConverter);

        // Give the activity descriptor a chance to customize the serializer options.
        clonedOptions = activityDescriptor?.ConfigureSerializerOptions?.Invoke(clonedOptions) ?? clonedOptions;

        WriteActivity(writer, value, clonedOptions, activityDescriptor, ignoreChildActivities);
    }

    private string GetActivityDetails(JsonElement activityRoot, out int activityTypeVersion, out ActivityDescriptor? activityDescriptor)
    {
        if (!activityRoot.TryGetProperty("type", out var activityTypeNameElement))
            throw new JsonException("Failed to extract activity type property");

        var activityTypeName = activityTypeNameElement.GetString()!;
        activityDescriptor = null;
        activityTypeVersion = 0;

        // First try and find the activity by its workflow definition version id. This is a special case when working with the WorkflowDefinitionActivity.
        if (activityRoot.TryGetProperty("workflowDefinitionVersionId", out var workflowDefinitionVersionIdElement))
        {
            var workflowDefinitionVersionId = workflowDefinitionVersionIdElement.GetString();
            activityDescriptor = _activityRegistry.Find(x =>
                x.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var value) && (string?)value == workflowDefinitionVersionId);
            activityTypeVersion = activityDescriptor?.Version ?? 0;
        }

        // If the activity type version is specified, use that to find the activity descriptor.
        if (activityDescriptor == null && activityRoot.TryGetProperty("version", out var activityVersionElement))
        {
            activityTypeVersion = activityVersionElement.GetInt32();
            activityDescriptor = _activityRegistry.Find(activityTypeName, activityTypeVersion);
        }

        // If the activity type version is not specified, use the latest version of the activity descriptor.
        if (activityDescriptor == null)
        {
            activityDescriptor = _activityRegistry.Find(activityTypeName);
            activityTypeVersion = activityDescriptor?.Version ?? 0;
        }

        return activityTypeName;
    }

    private void WriteActivity(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options, ActivityDescriptor? activityDescriptor, bool excludeChildren = false)
    {
        // Check if there's a specialized converter for the activity.
        var valueType = value.GetType();
        var specializedConverter = options.Converters.FirstOrDefault(x => x.CanConvert(valueType));
        if (specializedConverter != null)
        {
            JsonSerializer.Serialize(writer, value, valueType, options);
            return;
        }
        
        writer.WriteStartObject();

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;
            
            if (property.GetCustomAttribute<JsonIgnoreCompositeRootAttribute>() != null)
                continue;

            if (excludeChildren)
            {
                if (typeof(IActivity).IsAssignableFrom(property.PropertyType))
                    continue;

                if (typeof(IEnumerable<IActivity>).IsAssignableFrom(property.PropertyType))
                    continue;
            }

            var propName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            writer.WritePropertyName(propName);
            var input = property.GetValue(value);

            if (input == null)
            {
                writer.WriteNullValue();
                continue;
            }

            if (property.Name == nameof(IActivity.CustomProperties))
            {
                var customProperties = new Dictionary<string, object>(value.CustomProperties);
                foreach (var kvp in customProperties)
                {
                    if (kvp.Value is IActivity or IEnumerable<IActivity>)
                        customProperties.Remove(kvp.Key);
                }

                input = customProperties;
            }

            JsonSerializer.Serialize(writer, input, options);
        }

        WriteSyntheticProperties(writer, value, activityDescriptor, options);

        writer.WriteEndObject();
    }

    private void WriteSyntheticProperties(Utf8JsonWriter writer, IActivity value, ActivityDescriptor? activityDescriptor, JsonSerializerOptions options)
    {
        if (activityDescriptor == null)
        {
            _logger.LogDebug("No descriptor found for activity {ActivityType}", value.GetType().Name);
            return;
        }

        var syntheticInputs = activityDescriptor.Inputs.Where(x => x.IsSynthetic).ToList();
        var syntheticOutputs = activityDescriptor.Outputs.Where(x => x.IsSynthetic).ToList();

        // Write synthetic inputs. 
        foreach (var inputDescriptor in syntheticInputs)
        {
            var inputName = inputDescriptor.Name;
            var propertyName = inputName.Camelize();

            if (!value.SyntheticProperties.TryGetValue(inputName, out var inputValue))
                continue;

            writer.WritePropertyName(propertyName);

            var input = (Input?)inputValue;

            if (input == null)
            {
                writer.WriteNullValue();
                continue;
            }

            var expression = input.Expression;
            var expressionType = expression?.Type;
            var inputType = input.Type;
            var memoryReferenceId = input.MemoryBlockReference().Id;
            var expressionDescriptor = expressionType != null ? _expressionDescriptorRegistry.Find(expressionType) : default;

            if (expressionDescriptor == null)
                throw new Exception($"Syntax descriptor with expression type {expressionType} not found in registry");

            var inputModel = new
            {
                TypeName = inputType,
                Expression = expression,
                MemoryReference = new
                {
                    Id = memoryReferenceId
                }
            };

            JsonSerializer.Serialize(writer, inputModel, inputModel.GetType(), options);
        }

        // Write synthetic outputs. 
        foreach (var outputDescriptor in syntheticOutputs)
        {
            var outputName = outputDescriptor.Name;
            var propertyName = outputName.Camelize();

            if (!value.SyntheticProperties.TryGetValue(outputName, out var outputValue))
                continue;

            writer.WritePropertyName(propertyName);
            var output = (Output?)outputValue;

            if (output == null)
            {
                writer.WriteNullValue();
                continue;
            }

            var outputType = outputDescriptor.Type;
            var memoryReferenceId = output.MemoryBlockReference().Id;

            var outputModel = new
            {
                TypeName = outputType,
                MemoryReference = new
                {
                    Id = memoryReferenceId
                }
            };

            JsonSerializer.Serialize(writer, outputModel, outputModel.GetType(), options);
        }
    }

    private JsonSerializerOptions GetClonedOptions(JsonSerializerOptions options)
    {
        var clonedOptions = new JsonSerializerOptions(options);
        clonedOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        clonedOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));
        clonedOptions.Converters.Add(new ExpressionJsonConverterFactory(_expressionDescriptorRegistry));
        return clonedOptions;
    }
}