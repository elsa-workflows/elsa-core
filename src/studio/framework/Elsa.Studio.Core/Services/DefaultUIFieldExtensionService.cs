using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides a default implementation of UI field extension services.
/// </summary>
/// <param name="handlers"></param>
public class DefaultUIFieldExtensionService(IEnumerable<IUIFieldExtensionHandler> handlers) : IUIFieldExtensionService
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public List<IUIFieldExtensionHandler>? TryGetHandlers(string componentName, DisplayInputEditorContext context)
    {
        var activityName = context.ActivityDescriptor.Name;
        var syntax = context.InputDescriptor.DefaultSyntax ?? string.Empty;
        var matchingHandlers = handlers
            .Where(x => x.GetExtensionForInputComponent(componentName)
                && (!x.ActivityTypes.Any() || x.ActivityTypes.Contains(activityName))
                && (!x.Syntaxes.Any() || x.Syntaxes.Contains(syntax)))
            .OrderBy(x => x.DisplayOrder)
            .ToList();
        return matchingHandlers;
    }
}