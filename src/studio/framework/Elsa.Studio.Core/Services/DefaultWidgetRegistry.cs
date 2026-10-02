using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultWidgetRegistry : IWidgetRegistry
{
    private HashSet<IWidget> Widgets { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWidgetRegistry"/> class.
    /// </summary>
    public DefaultWidgetRegistry(IEnumerable<IWidget> widgets)
    {
        foreach (var widget in widgets) Add(widget);
    }

    /// <inheritdoc />
    public void Add(IWidget widget)
    {
        Widgets.Add(widget);
    }

    /// <inheritdoc />
    public IEnumerable<IWidget> List(string zone)
    {
        return Widgets.Where(x => x.Zone == zone).OrderBy(x => x.Order);
    }
}