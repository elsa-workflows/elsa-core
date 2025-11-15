using Elsa.Expressions.Models;
using Elsa.Expressions.Xs.Contracts;
using Elsa.Expressions.Xs.Models;
using Elsa.Expressions.Xs.Options;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Elsa.Expressions.Xs.Services;

/// <summary>
/// An XS expression evaluator using Hyperbee.XS.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XsEvaluator"/> class.
/// </remarks>
public class XsEvaluator(IOptions<XsOptions> options, IMemoryCache memoryCache) : IXsEvaluator
{
    private readonly XsOptions _xsOptions = options.Value;

    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(
        string expression,
        Type returnType,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default)
    {
        // Build the XS configuration
        var xsConfig = new XsConfig
        {
            Extensions = Hyperbee.Xs.Extensions.XsExtensions.Extensions().ToList()
        };

        // Apply configuration callbacks
        foreach (var callback in _xsOptions.ConfigureXsConfigCallbacks)
        {
            callback(xsConfig);
        }

        // Create type resolver with referenced assemblies
        var references = _xsOptions.Assemblies.ToArray();
        var typeResolver = TypeResolver.Create(references);

        // Get or create compiled delegate
        var compiledDelegate = GetCompiledDelegate(expression, returnType, xsConfig, typeResolver, context, options);

        // Execute the delegate
        return await Task.Run(() =>
        {
            try
            {
                return compiledDelegate.DynamicInvoke();
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap target invocation exception
                throw ex.InnerException;
            }
        }, cancellationToken);
    }

    private Delegate GetCompiledDelegate(string expression, Type returnType, XsConfig xsConfig, TypeResolver typeResolver, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var cacheKey = "xs:expression:" + Hash(expression);

        return memoryCache.GetOrCreate(cacheKey, entry =>
        {
            if (_xsOptions.ExpressionCacheTimeout.HasValue)
                entry.SetSlidingExpiration(_xsOptions.ExpressionCacheTimeout.Value);

            // Parse the XS expression
            var parser = new XsParser(xsConfig);
            var parsedExpression = parser.Parse(expression);

            // Create a lambda expression without parameters
            // TODO: Add support for accessing workflow context (globals) through expression transformation
            var lambda = LinqExpression.Lambda(parsedExpression);

            // Compile the lambda
            return lambda.Compile();
        })!;
    }

    private static string Hash(string expression)
    {
        var bytes = Encoding.UTF8.GetBytes(expression);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
