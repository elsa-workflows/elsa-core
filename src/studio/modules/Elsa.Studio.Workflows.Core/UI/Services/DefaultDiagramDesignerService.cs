using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.UI.Services;

/// <inheritdoc />
public class DefaultDiagramDesignerService : IDiagramDesignerService
{
    private readonly IReadOnlyList<IDiagramDesignerProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDiagramDesignerService"/> class.
    /// </summary>
    public DefaultDiagramDesignerService(IEnumerable<IDiagramDesignerProvider> providers)
    {
        _providers = providers.OrderByDescending(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public bool HasDiagramDesigner(JsonObject activity) => FindProvider(activity, false) != null;

    /// <inheritdoc />
    public IDiagramDesigner GetDiagramDesigner(JsonObject activity)
    {
        var provider = FindProvider(activity, false) ?? FindProvider(activity, true)
            ?? throw new Exception($"No diagram editor provider found for activity {activity.GetTypeName()}.");
        return provider.GetEditor();
    }

    private IDiagramDesignerProvider? FindProvider(JsonObject activity, bool includeFallback) =>
        _providers
            .FirstOrDefault(x => (includeFallback || !x.IsFallback) && x.GetSupportsActivity(activity));
}
