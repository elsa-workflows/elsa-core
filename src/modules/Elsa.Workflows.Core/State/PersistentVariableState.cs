using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.State;

public class PersistentVariableState
{
    [JsonConstructor]
    public PersistentVariableState()
    {
    }

    public PersistentVariableState(string name, string driveId)
    {
        Name = name;
        DriveId = driveId;
    }

    public string Name { get; set; } = default!;
    public string DriveId { get; set; } = default!;
}