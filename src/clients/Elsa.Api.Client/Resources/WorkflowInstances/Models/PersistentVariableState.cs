using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

public class PersistentVariableState
{
    [JsonConstructor]
    public PersistentVariableState()
    {
    }
    
    public string Name { get; set; } = default!;
    public string StorageDriverId { get; set; } = default!;
}