using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// (De)serializes objects of type <see cref="IActivity"/>.
/// </summary>
public class ActivityJsonConverter : JsonConverter<IActivity>
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IActivityFactory _activityFactory;
    private readonly IServiceProvider _serviceProvider;

    public ActivityJsonConverter(IActivityRegistry activityRegistry, IActivityFactory activityFactory, IServiceProvider serviceProvider)
    {
        _activityRegistry = activityRegistry;
        _activityFactory = activityFactory;
        _serviceProvider = serviceProvider;
    }

    public override IActivity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out var doc))
            throw new JsonException("Failed to parse JsonDocument");

        if (!doc.RootElement.TryGetProperty("type", out var activityTypeNameElement))
            throw new JsonException("Failed to extract activity type property");
        
        if (!doc.RootElement.TryGetProperty("version", out var activityTypeVersionElement))
            throw new JsonException("Failed to extract activity type version property");

        var activityTypeName = activityTypeNameElement.GetString()!;
        var activityTypeVersion = activityTypeVersionElement.GetInt32();
        var activityDescriptor = _activityRegistry.Find(activityTypeName, activityTypeVersion);

        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));
        
        if (activityDescriptor == null)
        {
            var notFoundContext = new ActivityConstructorContext(doc.RootElement, newOptions);
            var notFoundActivity =  (NotFoundActivity)_activityFactory.Create(typeof(NotFoundActivity), notFoundContext);

            notFoundActivity.Type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>();
            notFoundActivity.MissingTypeName = activityTypeName;
            notFoundActivity.MissingTypeVersion = activityTypeVersion;
            return notFoundActivity;
        }

        var context = new ActivityConstructorContext(doc.RootElement, newOptions);
        var activity = activityDescriptor.Constructor(context);

        return activity;
    }

    public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new InputJsonConverterFactory(_serviceProvider));
        newOptions.Converters.Add(new OutputJsonConverterFactory(_serviceProvider));
        JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
    }
}