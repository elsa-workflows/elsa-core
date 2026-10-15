using Elsa.Studio.Contracts;
using Elsa.Studio.PlatformIntegration.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.PlatformIntegration.Widgets;

/// <summary>
/// Renders Submit to Platform menu actions for workflow definition lists.
/// </summary>
public abstract class WorkflowDefinitionListSubmitWidgetBase : IWidget
{
    /// <inheritdoc />
    public abstract string Zone { get; }

    /// <inheritdoc />
    public double Order => 0;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        var sequence = 0;
        builder.OpenComponent<SubmitWorkflowDefinitionAction>(sequence++);
        builder.AddAttribute(sequence++, nameof(SubmitWorkflowDefinitionAction.DefinitionIds), GetDefinitionIds(attributes));
        builder.AddAttribute(sequence++, nameof(SubmitWorkflowDefinitionAction.Disabled), GetDisabled(attributes));
        builder.AddAttribute(sequence++, nameof(SubmitWorkflowDefinitionAction.RenderAsMenuItem), true);
        builder.CloseComponent();
    };

    private static IReadOnlyCollection<string> GetDefinitionIds(IDictionary<string, object?> attributes) =>
        attributes.TryGetValue("DefinitionIds", out var value) && value is IReadOnlyCollection<string> ids ? ids : [];

    private static bool GetDisabled(IDictionary<string, object?> attributes) =>
        attributes.TryGetValue("Disabled", out var value) && value is true;
}
