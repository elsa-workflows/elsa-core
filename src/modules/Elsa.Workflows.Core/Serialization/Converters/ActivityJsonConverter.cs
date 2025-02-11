using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Helpers;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter(
    IActivityRegistry activityRegistry,
    IActivityFactory activityFactory,
    IExpressionDescriptorRegistry expressionDescriptorRegistry,
    ActivityWriter activityWriter,
    IServiceProvider serviceProvider)
    : JsonConverter<IActivity>
{
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
            var notFoundActivityDescriptor = activityRegistry.Find<NotFoundActivity>()!;
            var notFoundContext = new ActivityConstructorContext(notFoundActivityDescriptor, activityRoot, clonedOptions);
            var notFoundActivity = (NotFoundActivity)activityFactory.Create(typeof(NotFoundActivity), notFoundContext);

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
        var activityDescriptor = activityRegistry.Find(value.Type, value.Version);

        // Give the activity descriptor a chance to customize the serializer options.
        clonedOptions = activityDescriptor?.ConfigureSerializerOptions?.Invoke(clonedOptions) ?? clonedOptions;

        activityWriter.WriteActivity(writer, value, clonedOptions);
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
            activityDescriptor = activityRegistry.Find(x =>
                x.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var value) && (string?)value == workflowDefinitionVersionId);
            activityTypeVersion = activityDescriptor?.Version ?? 0;
        }

        // If the activity type version is specified, use that to find the activity descriptor.
        if (activityDescriptor == null && activityRoot.TryGetProperty("version", out var activityVersionElement))
        {
            activityTypeVersion = activityVersionElement.GetInt32();
            activityDescriptor = activityRegistry.Find(activityTypeName, activityTypeVersion);
        }

        // If the activity type version is not specified, use the latest version of the activity descriptor.
        if (activityDescriptor == null)
        {
            activityDescriptor = activityRegistry.Find(activityTypeName);
            activityTypeVersion = activityDescriptor?.Version ?? 0;
        }

        return activityTypeName;
    }

    private JsonSerializerOptions GetClonedOptions(JsonSerializerOptions options)
    {
        var clonedOptions = new JsonSerializerOptions(options);
        clonedOptions.Converters.Add(new InputJsonConverterFactory(serviceProvider));
        clonedOptions.Converters.Add(new OutputJsonConverterFactory(serviceProvider));
        clonedOptions.Converters.Add(new ExpressionJsonConverterFactory(expressionDescriptorRegistry));
        return clonedOptions;
    }
}