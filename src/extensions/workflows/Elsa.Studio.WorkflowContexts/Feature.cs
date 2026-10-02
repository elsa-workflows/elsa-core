using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.WorkflowContexts.ActivityTabs;
using Elsa.Studio.WorkflowContexts.Widgets;

namespace Elsa.Studio.WorkflowContexts;

/// <summary>
/// Registers the workflow contexts feature.
/// </summary>
[RemoteFeature("Elsa.WorkflowContexts")]
public class Feature(IWidgetRegistry widgetRegistry, IActivityTabRegistry activityTabRegistry, ILocalizer localizer) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        widgetRegistry.Add(new WorkflowContextsEditorWidget());
        activityTabRegistry.Add(new WorkflowContextActivityTab(localizer));
        return default;
    }
}