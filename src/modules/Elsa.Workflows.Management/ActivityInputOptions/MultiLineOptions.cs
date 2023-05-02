using JetBrains.Annotations;

namespace Elsa.Workflows.Management.ActivityInputOptions;

/// <summary>
/// Options for the multi-line editor component.
/// </summary>
/// <param name="EditorHeight">The height of the editor.</param>
[PublicAPI]
public record MultiLineOptions(EditorHeight EditorHeight);