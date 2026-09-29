using Elsa.Studio.Workflows.Constants;

namespace Elsa.Studio.PlatformIntegration.Widgets;

/// <summary>
/// Renders the Submit to Platform action in each workflow definition row action menu.
/// </summary>
public sealed class WorkflowDefinitionListRowSubmitWidget : WorkflowDefinitionListSubmitWidgetBase
{
    /// <inheritdoc />
    public override string Zone => ZoneNames.WorkflowDefinitionListRowActions;
}
