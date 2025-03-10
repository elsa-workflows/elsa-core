using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Sql.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Providers;

/// <summary>
/// Provides SQL expression descriptors.
/// </summary>
public class SqlExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    /// <summary>
    /// Gets the name of the expression type.
    /// </summary>
    private const string TypeName = "Sql";

    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = TypeName,
            DisplayName = "SQL",
            Properties = new { MonacoLanguage = "sql" }.ToDictionary(),
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<SqlExpressionHandler>
        };
    }
}