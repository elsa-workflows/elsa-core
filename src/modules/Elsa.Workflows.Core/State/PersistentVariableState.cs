using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.State;

public class PersistentVariableState
{
    [JsonConstructor]
    public PersistentVariableState()
    {
    }

    public PersistentVariableState(string name, string storageDriverId)
    {
        Name = name;
        StorageDriverId = storageDriverId;
    }

    public string Name { get; set; } = default!;
    public string StorageDriverId { get; set; } = default!;
}