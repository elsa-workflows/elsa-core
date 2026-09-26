using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents an item in a data panel with label, text, link, and optional click action.
/// </summary>
/// <param name="Label">The display label for the item.</param>
/// <param name="Text">The optional text content for the item.</param>
/// <param name="Link">The optional link URL for the item.</param>
/// <param name="OnClick">The optional click handler for the item.</param>
/// <param name="LabelToolTip">The optional tooltip for the label.</param>
/// <param name="LabelComponent">The optional custom component to render in the label cell.</param>
/// <param name="Value">The raw value to be formatted and displayed.</param>
/// <param name="Format">The built-in display format to use for the value.</param>
/// <param name="FormatString">Optional format string for built-in formatters (e.g., "yyyy-MM-dd" for timestamps). Timestamps use the configured time formatter and default to "G"; DateTime values without a kind are treated as UTC.</param>
/// <param name="ValueTemplate">Custom render fragment for complete control over value display.</param>
/// <param name="ValueComponentType">Custom component type for reusable value renderers.</param>
public record DataPanelItem(
    string? Label = null,
    string? Text = null,
    string? Link = null,
    Func<Task>? OnClick = null,
    string? LabelToolTip = null,
    RenderFragment? LabelComponent = null,
    object? Value = null,
    DataPanelItemFormat Format = DataPanelItemFormat.Auto,
    string? FormatString = null,
    RenderFragment<DataPanelItemContext>? ValueTemplate = null,
    [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type? ValueComponentType = null);
