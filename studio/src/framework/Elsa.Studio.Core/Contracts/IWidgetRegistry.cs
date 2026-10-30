namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a registry of widgets.
/// </summary>
public interface IWidgetRegistry
{
    /// <summary>
    /// Adds the specified widget to the registry.
    /// </summary>
    void Add(IWidget widget);
    
    /// <summary>
    /// Returns a list of widgets for the specified zone.
    /// </summary>
    IEnumerable<IWidget> List(string zone);
}