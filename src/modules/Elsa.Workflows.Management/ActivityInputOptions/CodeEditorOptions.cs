using JetBrains.Annotations;

namespace Elsa.Workflows.Management.ActivityInputOptions;

/// <summary>
/// Options for the code editor component.
/// </summary>
/// <param name="EditorHeight">The height of the editor.</param>
/// <param name="Language">The language to use for syntax highlighting.</param>
/// <param name="SingleLineMode">Whether the editor should be in single line mode.</param>
[PublicAPI]
public record CodeEditorOptions(EditorHeight? EditorHeight = default, string? Language = default, bool? SingleLineMode = default);