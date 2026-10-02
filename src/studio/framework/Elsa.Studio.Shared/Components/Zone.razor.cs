using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents a zone in which widgets can be rendered.
/// </summary>
public partial class Zone : IDisposable
{
    /// <summary>
    /// Gets or sets the zone name.
    /// </summary>
    [Parameter]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the attributes that are passed to the widgets.
    /// </summary>
    [Parameter]
    public IDictionary<string, object?> Attributes { get; set; } = new Dictionary<string, object?>();

    [Inject] private IWidgetRegistry WidgetRegistry { get; set; } = default!;
    [Inject] private IFeatureService FeatureService { get; set; } = default!;
    private ICollection<IWidget> Widgets { get; set; } = new List<IWidget>();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        FeatureService.Initialized += OnFeatureServiceInitialized;
        Widgets = WidgetRegistry.List(Name).ToList();
    }

    private void OnFeatureServiceInitialized()
    {
        Widgets = WidgetRegistry.List(Name).ToList();
        StateHasChanged();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        FeatureService.Initialized -= OnFeatureServiceInitialized;
    }
}
