using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides the default implementation of the UI hint service.
/// </summary>
public class DefaultUIHintService(IEnumerable<IUIHintHandler> handlers) : IUIHintService
{
    /// <summary>
    /// Gets the handler.
    /// </summary>
    /// <param name="uiHint">The ui hint.</param>
    /// <returns>The result of the operation.</returns>
    public IUIHintHandler GetHandler(string uiHint)
    {
        var handler = handlers.FirstOrDefault(x => x.GetSupportsUIHint(uiHint));
        return handler ?? new UnsupportedUIHintHandler();
    }

    // public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    // {
    //     var handler = _handlers.FirstOrDefault(x => x.GetSupportsUIHint(context.InputDescriptor.UIHint));
    //     return handler == null ? DisplayUnsupportedUIHint(context) : handler.DisplayInputEditor(context);
    // }

    // private RenderFragment DisplayUnsupportedUIHint(DisplayInputEditorContext context)
    // {
    //     return builder =>
    //     {
    //         builder.OpenComponent<MudAlert>(0);
    //         builder.AddAttribute(1, nameof(MudAlert.Severity), Severity.Warning);
    //         builder.AddAttribute(2, nameof(MudAlert.Class), "my-2");
    //         builder.AddAttribute(3, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
    //         {
    //             textBuilder.AddContent(0, $"Unsupported UI hint: {context.InputDescriptor.UIHint}");
    //         }));
    //         builder.CloseElement();
    //     };
    // }

    
}