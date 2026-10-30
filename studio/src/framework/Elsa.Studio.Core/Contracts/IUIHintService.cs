namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines the contract for uihint service.
/// </summary>
public interface IUIHintService
{
    /// <summary>
    /// Gets the UI hint handler for the specified UI hint.
    /// </summary>
    IUIHintHandler GetHandler(string uiHint);
    //RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}
