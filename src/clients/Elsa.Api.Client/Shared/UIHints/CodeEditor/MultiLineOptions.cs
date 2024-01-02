using JetBrains.Annotations;

namespace Elsa.Api.Client.Shared.UIHints.CodeEditor;

/// <summary>
/// Options for the multi-line editor component.
/// </summary>
/// <param name="EditorHeight">The height of the editor.</param>
[PublicAPI]
public record MultiLineOptions(EditorHeight EditorHeight);