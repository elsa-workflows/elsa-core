using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

/// <summary>
/// Provides a handler for the <see cref="InputUIHints.OutcomePicker"/> UI hint.
/// </summary>
public class OutcomePickerHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint is InputUIHints.OutcomePicker;

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(OutcomePicker));
            builder.AddAttribute(1, nameof(OutcomePicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}