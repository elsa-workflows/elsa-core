using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.PlatformIntegration.Components;
using Elsa.Studio.Workflows.Constants;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.PlatformIntegration.Widgets;

/// <summary>
/// Renders the Submit to Platform action in the workflow definition editor toolbar.
/// </summary>
public sealed class WorkflowDefinitionEditorSubmitWidget : IWidget
{
    /// <inheritdoc />
    public string Zone => ZoneNames.WorkflowDefinitionEditorToolbarActions;

    /// <inheritdoc />
    public double Order => 0;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        var getWorkflowDefinition = GetWorkflowDefinition(attributes);
        var sequence = 0;
        builder.OpenComponent<SubmitWorkflowDefinitionAction>(sequence++);
        builder.AddAttribute(sequence++, nameof(SubmitWorkflowDefinitionAction.GetWorkflowDefinition), getWorkflowDefinition);
        builder.AddAttribute(sequence++, nameof(SubmitWorkflowDefinitionAction.Disabled), getWorkflowDefinition is null);
        builder.CloseComponent();
    };

    private static Func<Task<WorkflowDefinition?>>? GetWorkflowDefinition(IDictionary<string, object?> attributes) =>
        attributes.TryGetValue("GetWorkflowDefinition", out var value)
            ? value as Func<Task<WorkflowDefinition?>>
            : null;
}
