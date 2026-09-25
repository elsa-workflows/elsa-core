using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

/// <summary>
/// Provides a handler for the <see cref="InputUIHints.MultiLine"/> UI hint.
/// </summary>
public class MultiLineHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint is InputUIHints.MultiLine;

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MultiLine));
            builder.AddAttribute(1, nameof(MultiLine.EditorContext), context);
            builder.CloseComponent();
        };
    }
}