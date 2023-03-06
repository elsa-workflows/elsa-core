using System.ComponentModel.DataAnnotations;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A data drive that stores objects in the workflow state itself.
/// </summary>
[Display(Name = "Workflow")]
public class WorkflowStorageDriver : IStorageDriver
{
    public const string VariablesDictionaryStateKey = "PersistentVariablesDictionary";
    
    public ValueTask WriteAsync(string id, object value, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary[id] = value);
        return ValueTask.CompletedTask;
    }

    public ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
    {
        var dictionary = GetVariablesDictionary(context);
        var value = dictionary.TryGetValue(id, out var v) ? v : default;
        return new(value);
    }

    public ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary.Remove(id));
        return ValueTask.CompletedTask;
    }

    private IDictionary<string, object> GetVariablesDictionary(StorageDriverContext context) => context.WorkflowExecutionContext.Properties.GetOrAdd(VariablesDictionaryStateKey, () => new Dictionary<string, object>());
    private void SetVariablesDictionary(StorageDriverContext context, IDictionary<string, object> dictionary) => context.WorkflowExecutionContext.Properties[VariablesDictionaryStateKey] = dictionary;

    private void UpdateVariablesDictionary(StorageDriverContext context, Action<IDictionary<string, object>> update)
    {
        var dictionary = GetVariablesDictionary(context);
        update(dictionary);
        SetVariablesDictionary(context, dictionary);
    }
}