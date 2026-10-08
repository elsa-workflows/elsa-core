using JetBrains.Annotations;

namespace Elsa.Workflows.UIHints.CodeEditor;

/// <summary>
/// Options for the code editor component.
/// </summary>
[PublicAPI]
public class CodeEditorOptions
{
    /// <summary>The height of the editor.</summary>
    public EditorHeight? EditorHeight { get; set; }

    /// <summary>The language to use for syntax highlighting.</summary>
    public string? Language { get; set; }

    /// <summary>Whether the editor should be in single line mode.</summary>
    public bool? SingleLineMode { get; set; }
}