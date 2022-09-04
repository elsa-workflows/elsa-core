using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Comparers;

public class WorkflowTriggerHashEqualityComparer : IEqualityComparer<WorkflowTrigger>
{
    public bool Equals(WorkflowTrigger? x, WorkflowTrigger? y) => x?.Hash?.Equals(y?.Hash) ?? false;
    public int GetHashCode(WorkflowTrigger obj) => obj.Hash?.GetHashCode() ?? "".GetHashCode();
}