using Elsa.Studio.Contracts;
using Elsa.Studio.Enums;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Abstractions;

/// <summary>
/// Provides a base handler for field extensions.
/// </summary>
public abstract class FieldExtensionHandlerBase : IUIFieldExtensionHandler
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual int DisplayOrder { get; set; } = 1;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual bool IncludeForAll { get; set; } = false;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual FieldExtensionPosition Position { get; set; } = FieldExtensionPosition.Top;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual string UIHintComponent { get; set; } = string.Empty;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual List<string> ActivityTypes { get; set; } = [];

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual List<string> Syntaxes { get; set; } = [];

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public virtual bool GetExtensionForInputComponent(string componentName) =>
        (string.IsNullOrEmpty(UIHintComponent) && !ActivityTypes.Any() && !Syntaxes.Any() && IncludeForAll)
            || UIHintComponent.Equals(componentName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public abstract RenderFragment DisplayExtension(DisplayInputEditorContext context);
}