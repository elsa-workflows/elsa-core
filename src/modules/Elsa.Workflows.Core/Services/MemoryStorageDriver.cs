using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A storage driver that stores objects in memory.
/// </summary>
[Display(Name = "Memory")]
public class MemoryStorageDriver : IStorageDriver
{
    private readonly IDictionary<string, object> _dictionary = new Dictionary<string, object>();

    /// <inheritdoc />
    public ValueTask WriteAsync(string id, object value, StorageDriverContext context)
    {
        _dictionary[id] = value;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
    {
        var value = _dictionary.TryGetValue(id, out var v) ? v : default;
        return new (value);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        _dictionary.Remove(id);
        return ValueTask.CompletedTask;
    }
}