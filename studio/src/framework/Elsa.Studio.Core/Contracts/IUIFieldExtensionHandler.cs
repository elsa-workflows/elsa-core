using Elsa.Studio.Enums;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a contract for UI field extension handlers.
/// </summary>
public interface IUIFieldExtensionHandler
{
    /// <summary>
    /// The order in which the extensions should be displayed.
    /// </summary>
    int DisplayOrder { get; set; }

    /// <summary>
    /// If set to true and no restricting types are set, either with <c>UIHintComponent</c>, <c>ActivityTypes</c> or <c>Syntaxes</c>, this extension will be rendered for in all field types.
    /// </summary>
    bool IncludeForAll { get; set; }

    /// <summary>
    /// The position to render the extension within the field.
    /// </summary>
    FieldExtensionPosition Position { get; set; }

    /// <summary>
    /// The UIHint component this extension should be rendered for.
    /// </summary>
    string UIHintComponent { get; set; }

    /// <summary>
    /// The activities this extension should be rendered for.
    /// </summary>
    List<string> ActivityTypes { get; set; }

    /// <summary>
    /// The syntaxes this extension should be rendered for.
    /// </summary>
    List<string> Syntaxes { get; set; }

    /// <summary>
    /// Returns true if the handler extension the specified or is empty.
    /// </summary>
    bool GetExtensionForInputComponent(string componentName);

    /// <summary>
    /// Returns a <see cref="RenderFragment"/> of the added extension.
    /// </summary>
    RenderFragment DisplayExtension(DisplayInputEditorContext context);
}