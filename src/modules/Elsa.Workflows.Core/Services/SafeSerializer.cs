using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class SafeSerializer : ISafeSerializer
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeSerializer"/> class.
    /// </summary>
    public SafeSerializer(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    /// <inheritdoc />
    public async ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default)
    {
        var options = CreateOptions();
        var notification = new SafeSerializerSerializing(value, options);
        await _notificationSender.SendAsync(notification, cancellationToken);
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    public async ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default)
    {
        var options = CreateOptions();
        var notification = new SafeSerializerSerializing(value, options);
        await _notificationSender.SendAsync(notification, cancellationToken);
        return JsonSerializer.SerializeToElement(value, options);
    }

    /// <inheritdoc />
    public async ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        var options = CreateOptions();
        var notification = new SafeSerializerDeserializing(json, options);
        await _notificationSender.SendAsync(notification, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }
    
    private JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()),
                new SafeValueConverterFactory()
            }
        };

        return options;
    }
}