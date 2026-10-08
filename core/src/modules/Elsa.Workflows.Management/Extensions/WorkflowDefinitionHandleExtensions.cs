using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management;

public static class WorkflowDefinitionHandleExtensions
{
    public static WorkflowDefinitionFilter ToFilter(this WorkflowDefinitionHandle handle)
    {
        return new()
        {
            DefinitionId = handle.DefinitionId,
            Id = handle.DefinitionVersionId,
            VersionOptions = handle.VersionOptions
        };
    }
}