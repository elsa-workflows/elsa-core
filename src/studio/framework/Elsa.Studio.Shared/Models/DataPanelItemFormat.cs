namespace Elsa.Studio.Models;

/// <summary>
/// Defines the built-in display formats for data panel item values.
/// </summary>
public enum DataPanelItemFormat
{
    /// <summary>
    /// Automatically infer the format from the value type.
    /// </summary>
    Auto,

    /// <summary>
    /// Display as plain text.
    /// </summary>
    Text,

    /// <summary>
    /// Display as a formatted timestamp.
    /// </summary>
    Timestamp,

    /// <summary>
    /// Display as a number with thousand separators.
    /// </summary>
    Number,

    /// <summary>
    /// Display as Yes/No or checkmark.
    /// </summary>
    Boolean,

    /// <summary>
    /// Display as pretty-printed JSON.
    /// </summary>
    Json,

    /// <summary>
    /// Render as markdown.
    /// </summary>
    Markdown,

    /// <summary>
    /// Display as syntax-highlighted code.
    /// </summary>
    Code
}
