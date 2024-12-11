using System.ComponentModel.DataAnnotations;
using Elsa.Extensions;


// ReSharper disable once CheckNamespace
// Backwards compatibility for the old namespace. 
namespace Elsa.Workflows.Services;

/// <summary>
/// A storage driver that stores objects in the workflow state itself.
/// </summary>
[Display(Name = "Workflow")]
[Obsolete("This is no longer used and will be removed in a future version. Use the WorkflowInstanceStorageDriver instead.")]
public class WorkflowStorageDriver : IStorageDriver
{
    /// <summary>
    /// The key used to store the variables in the workflow state.
    /// </summary>
    public const string VariablesDictionaryStateKey = "PersistentVariablesDictionary";

    /// <inheritdoc />
    public double Priority => -1;
    /// <inheritdoc />
    public IEnumerable<string> Tags => [];

    /// <inheritdoc />
    public ValueTask WriteAsync(string id, object value, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary[id] = value);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
    {
        var dictionary = GetVariablesDictionary(context);
        var value = dictionary.TryGetValue(id, out var v) ? v : default;
        return new(value);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary.Remove(id));
        return ValueTask.CompletedTask;
    }

    private IDictionary<string, object> GetVariablesDictionary(StorageDriverContext context) => context.ExecutionContext.Properties.GetOrAdd(VariablesDictionaryStateKey, () => new Dictionary<string, object>());
    private void SetVariablesDictionary(StorageDriverContext context, IDictionary<string, object> dictionary) => context.ExecutionContext.Properties[VariablesDictionaryStateKey] = dictionary;

    private void UpdateVariablesDictionary(StorageDriverContext context, Action<IDictionary<string, object>> update)
    {
        var dictionary = GetVariablesDictionary(context);
        update(dictionary);
        SetVariablesDictionary(context, dictionary);
    }
}