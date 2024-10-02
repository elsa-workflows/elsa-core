using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.ResourceManagement.Metadata.Models;

public abstract class ResourceDefinition
{
    private Dictionary<Type, object> _namedSettings = [];
    public JsonObject? Settings { get; set; }

    public string Name { get; protected set; } = string.Empty;

    public T GetSettings<T>() where T : new()
    {
        if (Settings == null!)
            return new T();

        var namedSettings = _namedSettings;

        if (!namedSettings.TryGetValue(typeof(T), out var result))
        {
            var typeName = typeof(T).Name;
            result = Settings.TryGetPropertyValue(typeName, out var value) ? value.Deserialize<T>() : new T();

            namedSettings = new Dictionary<Type, object>(_namedSettings)
            {
                [typeof(T)] = result!
            };

            _namedSettings = namedSettings;
        }

        return (T)result!;
    }
}