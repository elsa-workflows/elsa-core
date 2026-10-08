using Elsa.Api.Client.Resources.Scripting.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.Scripting.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Expression"/>.
/// </summary>
[PublicAPI]
public static class ExpressionDescriptorExtensions
{
    /// <summary>
    /// Gets the expression language to use for the Monaco editor.
    /// </summary>
    public static string? GetMonacoLanguage(this ExpressionDescriptor descriptor) => descriptor.Properties.TryGetValue("MonacoLanguage", out var language) ? language : null;
}