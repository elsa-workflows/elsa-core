using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties;

/// <summary>
/// Represents the workflow properties.
/// </summary>
public partial class WorkflowProperties
{
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
}