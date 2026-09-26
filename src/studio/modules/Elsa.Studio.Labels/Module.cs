using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Widgets;

namespace Elsa.Studio.Labels;

/// <summary>
/// Represents a feature that initializes and registers widgets for workflow definition labels.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Labels.ShellFeatures.Labels";

    private readonly IWidgetRegistry _widgetRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="Feature"/> class.
    /// </summary>
    /// <param name="widgetRegistry">The widget registry to register widgets with.</param>
    public Feature(IWidgetRegistry widgetRegistry)
    {
        _widgetRegistry = widgetRegistry;
    }

    /// <summary>
    /// Initializes the feature by registering the <see cref="WorkflowDefinitionLabelsEditorWidget"/> widget.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _widgetRegistry.Add(new WorkflowDefinitionLabelsEditorWidget());
        return base.InitializeAsync(cancellationToken);
    }
}
