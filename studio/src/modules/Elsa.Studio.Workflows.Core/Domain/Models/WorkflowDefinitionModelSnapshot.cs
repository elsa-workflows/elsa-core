using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Copies workflow metadata while taking an independent snapshot of its mutable root.
/// </summary>
internal static class WorkflowDefinitionModelSnapshot
{
    public static WorkflowDefinitionModel WithRoot(WorkflowDefinitionModel model, JsonObject? root) => new()
    {
        Id = model.Id,
        CreatedAt = model.CreatedAt,
        Version = model.Version,
        IsLatest = model.IsLatest,
        IsPublished = model.IsPublished,
        Links = model.Links,
        DefinitionId = model.DefinitionId,
        Name = model.Name,
        Description = model.Description,
        ToolVersion = model.ToolVersion,
        Variables = model.Variables,
        Inputs = model.Inputs,
        Outputs = model.Outputs,
        Outcomes = model.Outcomes,
        CustomProperties = model.CustomProperties,
        IsReadonly = model.IsReadonly,
        Options = model.Options,
        Root = root?.DeepClone().AsObject()
    };
}
