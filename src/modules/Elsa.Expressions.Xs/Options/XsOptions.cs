using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Expressions.Xs.Models;
using Hyperbee.XS;

namespace Elsa.Expressions.Xs.Options;

/// <summary>
/// Options for the XS expression evaluator.
/// </summary>
public class XsOptions
{
    /// <summary>
    /// A list of callbacks that is invoked when an XS expression is evaluated. Use this to configure the <see cref="XsConfig"/>.
    /// </summary>
    public ICollection<Action<XsConfig>> ConfigureXsConfigCallbacks { get; } = new List<Action<XsConfig>>();

    /// <summary>
    /// A list of assemblies that will be referenced when evaluating an XS expression.
    /// </summary>
    public ISet<Assembly> Assemblies { get; } = new HashSet<Assembly>(new[]
    {
        typeof(Globals).Assembly, // Elsa.Expressions.Xs
        typeof(Enumerable).Assembly, // System.Linq
        typeof(Guid).Assembly, // System.Runtime
        typeof(JsonSerializer).Assembly, // System.Text.Json
        typeof(IDictionary<string, object>).Assembly, // System.Collections
    });

    /// <summary>
    /// The timeout for expression caching.
    /// </summary>
    /// <remarks>
    /// The <c>ExpressionCacheTimeout</c> property specifies the duration for which the expressions are cached in the XS engine. When an expression is executed, it is compiled and cached for future use. This caching improves performance by avoiding repetitive compilation of the same expression.
    /// If the value of <c>ExpressionCacheTimeout</c> is <c>null</c>, the expressions are cached indefinitely. If a time value is specified, the expressions will be purged from the cache after they've been unused for the specified duration and recompiled on next use.
    /// </remarks>
    public TimeSpan? ExpressionCacheTimeout { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Configures the <see cref="XsConfig"/>.
    /// </summary>
    /// <param name="configurator">A callback that is invoked before an XS expression is evaluated. Use this to configure the <see cref="XsConfig"/>.</param>
    public XsOptions ConfigureXsConfig(Action<XsConfig> configurator)
    {
        ConfigureXsConfigCallbacks.Add(configurator);
        return this;
    }
}
