using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Workflows;

/// <summary>
/// A storage driver that stores objects in the workflow state itself.
/// </summary>
[Display(Name = "Workflow Instance")]
[UsedImplicitly]
public class WorkflowInstanceStorageDriver : IStorageDriver
{
    /// <summary>
    /// The key used to store the variables in the workflow state.
    /// </summary>
    public const string VariablesDictionaryStateKey = "Variables";

    public double Priority => 5;

    /// <inheritdoc />
    public ValueTask WriteAsync(string id, object value, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary =>
        {
            var node = JsonSerializer.SerializeToNode(value);
            dictionary[id] = node;
        });
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
    {
        var dictionary = GetVariablesDictionary(context);
        var node = dictionary.GetValueOrDefault(id);
        return new(node);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        UpdateVariablesDictionary(context, dictionary => dictionary.Remove(id));
        return ValueTask.CompletedTask;
    }

    private VariablesDictionary GetVariablesDictionary(StorageDriverContext context) => context.ExecutionContext.Properties.GetOrAdd(VariablesDictionaryStateKey, () => new VariablesDictionary());
    private void SetVariablesDictionary(StorageDriverContext context, VariablesDictionary dictionary) => context.ExecutionContext.Properties[VariablesDictionaryStateKey] = dictionary;

    private void UpdateVariablesDictionary(StorageDriverContext context, Action<VariablesDictionary> update)
    {
        var dictionary = GetVariablesDictionary(context);
        update(dictionary);
        SetVariablesDictionary(context, dictionary);
    }
}

public class VariablesDictionary : Dictionary<string, JsonNode>;