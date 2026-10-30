using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a contract for UI hint handlers.
/// </summary>
public interface IUIHintHandler
{
    /// <summary>
    /// Returns true if the handler supports the specified UI hint.
    /// </summary>
    bool GetSupportsUIHint(string uiHint);
    
    /// <summary>
    /// Returns the UI syntax for the handler.
    /// </summary>
    string UISyntax { get; }
    
    /// <summary>
    /// Returns a <see cref="RenderFragment"/> that renders the input editor.
    /// </summary>
    RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}