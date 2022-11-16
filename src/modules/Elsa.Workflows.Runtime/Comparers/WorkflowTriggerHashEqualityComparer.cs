using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Comparers;

public class WorkflowTriggerHashEqualityComparer : IEqualityComparer<StoredTrigger>
{
    public bool Equals(StoredTrigger? x, StoredTrigger? y) => x?.Hash?.Equals(y?.Hash) ?? false;
    public int GetHashCode(StoredTrigger obj) => obj.Hash?.GetHashCode() ?? "".GetHashCode();
}