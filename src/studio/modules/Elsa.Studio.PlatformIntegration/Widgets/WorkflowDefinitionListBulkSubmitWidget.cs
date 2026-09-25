using Elsa.Studio.Workflows.Constants;

namespace Elsa.Studio.PlatformIntegration.Widgets;

/// <summary>
/// Renders the Submit to Platform action in the workflow definition list bulk actions menu.
/// </summary>
public sealed class WorkflowDefinitionListBulkSubmitWidget : WorkflowDefinitionListSubmitWidgetBase
{
    /// <inheritdoc />
    public override string Zone => ZoneNames.WorkflowDefinitionListBulkActions;
}
