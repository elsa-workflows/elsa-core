using Elsa.Sql.Contracts;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Sql.Expressions;

/// <summary>
/// Evaluates SQL expressions.
/// </summary>
public class SqlExpressionHandler : IExpressionHandler
{
    private readonly ISqlEvaluator _sqlEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExpressionHandler"/> class.
    /// </summary>
    public SqlExpressionHandler(ISqlEvaluator sqlEvaluator)
    {
        _sqlEvaluator = sqlEvaluator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        var script = expression.Value.ConvertTo<string>() ?? "";
        return await _sqlEvaluator.EvaluateAsync(script, context, options);
    }
}