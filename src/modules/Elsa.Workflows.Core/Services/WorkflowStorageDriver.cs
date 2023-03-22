using System.ComponentModel.DataAnnotations;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A storage driver that stores objects in the workflow state itself.
/// </summary>
[Display(Name = "Workflow")]
public class WorkflowStorageDriver : IStorageDriver
{
    /// <summary>
    /// The key used to store the variables propertyBag in the workflow state.
    /// </summary>
    public const string VariablesDictionaryStateKey = "PersistentVariablesDictionary";

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
        var value = dictionary.Dictionary.TryGetValue(id, out var v) ? v : default;
        return new(value);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary.Remove(id));
        return ValueTask.CompletedTask;
    }

    private PropertyBag GetVariablesDictionary(StorageDriverContext context) => context.ExecutionContext.Properties.GetOrAdd(VariablesDictionaryStateKey, () => new PropertyBag());
    private void SetVariablesDictionary(StorageDriverContext context, PropertyBag propertyBag) => context.ExecutionContext.Properties[VariablesDictionaryStateKey] = propertyBag;

    private void UpdateVariablesDictionary(StorageDriverContext context, Action<IDictionary<string, object>> update)
    {
        var dictionary = GetVariablesDictionary(context);
        update(dictionary.Dictionary);
        SetVariablesDictionary(context, dictionary);
    }
}