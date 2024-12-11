using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using System.Text.RegularExpressions;

namespace Trimble.Elsa.Activities.Activities.Expressions;

/// <summary>
/// Runs a regex match against a variable string value. 
/// Adds matching patterns as an activity result for consumption in a switch activity.
/// </summary>
public class RegexVariableExpressionHandler : IExpressionHandler
{
    /// <summary>
    /// Returns a bool indicating whether or not the expression matched
    /// </summary>
    public ValueTask<object?> EvaluateAsync(
        Expression expression, 
        Type returnType, 
        ExpressionExecutionContext context, 
        ExpressionEvaluatorOptions options)
    {
        // Must return a bool? to be consumed in FlowMatch
        var comparator = expression.Value.ConvertTo<VariableMatchCondition>();

        if (comparator == null)
        {
            return new ValueTask<object?>(false);
        }

        var regexMatcher = new Regex(comparator.matchPattern);
        var sourceVariable = context.GetVariable(comparator.variableName);
        
        if (sourceVariable == null)
        {
            return new ValueTask<object?>(false);
        }

        var variableValue = context.Get(sourceVariable)?.ToString() ?? string.Empty;

        bool matched = regexMatcher.IsMatch(variableValue);

        context.GetActivityExecutionContext()?.LogDebug<SendHttpRequestMapper>(
               "Matching variable value.", 
               new {
                       Regex = comparator.matchPattern,
                       Variable = sourceVariable?.Name,
                       IsMatch = matched
                   });

        return new ValueTask<object?>(matched);
    }
}
