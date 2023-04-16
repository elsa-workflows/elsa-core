using Elsa.Expressions.Options;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

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