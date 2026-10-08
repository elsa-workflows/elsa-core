using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Services;

/// <summary>
/// A fallback UI handler that is used when no other handler is found for a given UI hint.
/// </summary>
public class UnsupportedUIHintHandler : IUIHintHandler
{
    /// <summary>
    /// Provides the get supports uihint.
    /// </summary>
    public bool GetSupportsUIHint(string uiHint) => false;

    /// <summary>
    /// Provides the uisyntax.
    /// </summary>
    public string UISyntax => "Unsupported";
    
    /// <summary>
    /// Performs the display input editor operation.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The result of the operation.</returns>
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent<MudAlert>(0);
            builder.AddAttribute(1, nameof(MudAlert.Severity), Severity.Warning);
            builder.AddAttribute(2, nameof(MudAlert.Class), "my-2");
            builder.AddAttribute(3, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
            {
                textBuilder.AddContent(0, $"Unsupported UI hint: {context.InputDescriptor.UIHint}");
            }));
            builder.CloseElement();
        };
    }
}