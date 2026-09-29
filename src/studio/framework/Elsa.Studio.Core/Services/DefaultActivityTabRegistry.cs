using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultActivityTabRegistry : IActivityTabRegistry
{
    private HashSet<IActivityTab> ActivityTabs { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityTabRegistry"/> class.
    /// </summary>
    public DefaultActivityTabRegistry(IEnumerable<IActivityTab> widgets)
    {
        foreach (var widget in widgets) Add(widget);
    }

    /// <inheritdoc />
    public void Add(IActivityTab tab)
    {
        ActivityTabs.Add(tab);
    }

    /// <inheritdoc />
    public IEnumerable<IActivityTab> List()
    {
        return ActivityTabs.OrderBy(x => x.Order);
    }
}