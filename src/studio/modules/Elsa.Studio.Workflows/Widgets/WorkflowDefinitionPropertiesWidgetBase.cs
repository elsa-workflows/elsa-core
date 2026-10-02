using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Constants;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Widgets;

/// <summary>
/// A widget that renders the workflow definition info.
/// </summary>
public abstract class WorkflowDefinitionPropertiesWidgetBase : IWidget
{
    /// <inheritdoc />
    public string Zone => ZoneNames.WorkflowDefinitionProperties;

    /// <inheritdoc />
    public virtual double Order => 0;

    /// <inheritdoc />
    public abstract Func<IDictionary<string, object?>, RenderFragment> Render { get; }
}