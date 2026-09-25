using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Components;
using Elsa.Studio.Secrets.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Secrets.Handlers;

public class SecretPickerHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => string.Equals(uiHint, SecretInputUIHints.SecretPicker, StringComparison.OrdinalIgnoreCase);

    public string UISyntax => SecretExpressionTypes.Secret;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(SecretPickerInput));
            builder.AddAttribute(1, nameof(SecretPickerInput.EditorContext), context);
            builder.CloseComponent();
        };
    }
}
