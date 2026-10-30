using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a service contact for resolving UI field extensions
/// </summary>
public interface IUIFieldExtensionService
{
    /// <summary>
    /// Try to return a list of extension handlers for a component
    /// </summary>
    /// <param name="componentName">The component name to get the handler for. This should be the name of the component calling this function.</param>
    /// <param name="context">The display input editor context</param>
    /// <returns></returns>
    List<IUIFieldExtensionHandler>? TryGetHandlers(string componentName, DisplayInputEditorContext context);
}