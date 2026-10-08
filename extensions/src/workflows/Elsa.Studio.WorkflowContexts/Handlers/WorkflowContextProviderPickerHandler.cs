using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using Elsa.Studio.WorkflowContexts.Components;

namespace Elsa.Studio.WorkflowContexts.Handlers;

public class WorkflowContextProviderPickerHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint == "workflow-context-provider-picker";

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(WorkflowContextProviderPicker));
            builder.AddAttribute(1, nameof(WorkflowContextProviderPicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}
