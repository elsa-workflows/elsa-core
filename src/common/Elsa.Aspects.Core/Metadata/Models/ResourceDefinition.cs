using System.Text.Json.Nodes;

namespace Elsa.Aspects.Metadata;

public abstract class ResourceDefinition
{
    public string Name { get; protected set; } = default!;

    private Dictionary<Type, object> _typedSettings = [];

    protected JsonObject? Settings { get; set; }

    public T GetSettings<T>() where T : new()
    {
        if (Settings == null)
        {
            return new T();
        }

        var namedSettings = _typedSettings;

        if (!namedSettings.TryGetValue(typeof(T), out var result))
        {
            var typeName = typeof(T).Name;

            result = Settings.TryGetPropertyValue(typeName, out var value) ? value.ToObject<T>()! : new T();

            namedSettings = new Dictionary<Type, object>(_typedSettings)
            {
                [typeof(T)] = result,
            };

            _typedSettings = namedSettings;
        }

        return (T)result;
    }
}