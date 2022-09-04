using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Extensions;

public static class WorkflowDefinitionExtensions
{
    public static Workflow ToWorkflow(this WorkflowDefinition definition, IActivity root)
    {
        return new Workflow(
            new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id),
            new WorkflowPublication(definition.IsLatest, definition.IsPublished),
            new WorkflowMetadata(definition.Name, definition.Description, definition.CreatedAt),
            root,
            definition.Variables,
            definition.Metadata,
            definition.ApplicationProperties);
    }
}