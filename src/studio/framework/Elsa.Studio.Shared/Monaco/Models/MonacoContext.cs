using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents a context for working with the Monaco editor.
/// </summary>
public record MonacoContext(StandaloneCodeEditor Editor, ExpressionDescriptor ExpressionDescriptor, IDictionary<string, object> CustomProperties);