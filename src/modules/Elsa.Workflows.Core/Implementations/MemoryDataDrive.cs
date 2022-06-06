using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class MemoryDataDrive : IDataDrive
{
    private readonly IDictionary<string, object> _dictionary = new Dictionary<string, object>();
    public string Id => DataDriveNames.Memory;

    public ValueTask WriteAsync(string id, object value, DataDriveContext context)
    {
        _dictionary[id] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask<object?> ReadAsync(string id, DataDriveContext context)
    {
        var value = _dictionary.TryGetValue(id, out var v) ? v : default;
        return new (value);
    }

    public ValueTask DeleteAsync(string id, DataDriveContext context)
    {
        _dictionary.Remove(id);
        return ValueTask.CompletedTask;
    }
}