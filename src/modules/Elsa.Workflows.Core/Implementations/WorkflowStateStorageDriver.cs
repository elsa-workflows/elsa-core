using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

/// <summary>
/// A data drive that stores objects in the workflow state itself.
/// </summary>
public class WorkflowStateStorageDriver : IStorageDriver
{
    public const string VariablesDictionaryStateKey = "PersistentVariablesDictionary";
    public string Id => StorageDriverNames.Workflow;
    public string DisplayName => StorageDriverNames.Workflow;

    public ValueTask WriteAsync(string id, object value, DataDriveContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary[id] = value);
        return ValueTask.CompletedTask;
    }

    public ValueTask<object?> ReadAsync(string id, DataDriveContext context)
    {
        var dictionary = GetVariablesDictionary(context);
        var value = dictionary.TryGetValue(id, out var v) ? v : default;
        return new(value);
    }

    public ValueTask DeleteAsync(string id, DataDriveContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary.Remove(id));
        return ValueTask.CompletedTask;
    }

    private IDictionary<string, object> GetVariablesDictionary(DataDriveContext context) => context.WorkflowState.Properties.GetOrAdd(VariablesDictionaryStateKey, () => new Dictionary<string, object>());
    private void SetVariablesDictionary(DataDriveContext context, IDictionary<string, object> dictionary) => context.WorkflowState.Properties[VariablesDictionaryStateKey] = dictionary;

    private void UpdateVariablesDictionary(DataDriveContext context, Action<IDictionary<string, object>> update)
    {
        var dictionary = GetVariablesDictionary(context);
        update(dictionary);
        SetVariablesDictionary(context, dictionary);
    }
}