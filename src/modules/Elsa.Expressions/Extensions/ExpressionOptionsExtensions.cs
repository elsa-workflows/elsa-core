using Elsa.Expressions.Options;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="ExpressionOptions"/>.
/// </summary>
[PublicAPI]
public static class ExpressionOptionsExtensions
{
    /// <summary>
    /// Register type <see cref="T"/> with the specified alias.
    /// </summary>
    public static void AddTypeAlias<T>(this ExpressionOptions options, string alias) => options.RegisterTypeAlias(typeof(T), alias);
}