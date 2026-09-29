using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

/// <summary>
/// Provides a handler for the <see cref="InputUIHints.FlowSwitchEditor"/> and <see cref="InputUIHints.SwitchEditor"/> UI hints.
/// </summary>
public class SwitchEditorHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint is InputUIHints.FlowSwitchEditor or InputUIHints.SwitchEditor;

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Object;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Cases));
            builder.AddAttribute(1, nameof(Cases.EditorContext), context);
            builder.CloseComponent();
        };
    }
}