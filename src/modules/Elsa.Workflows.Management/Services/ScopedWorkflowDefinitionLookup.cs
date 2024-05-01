using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Management.Services;

public class ScopedWorkflowDefinitionLookup
{
    public HashSet<Workflow> Workflows { get; set; } = new();
}