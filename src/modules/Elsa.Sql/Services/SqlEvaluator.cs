using System.Text;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Sql.Contracts;
using Elsa.Sql.Models;

namespace Elsa.Sql.Services;

/// <summary>
/// A SQL expression evaluator.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SqlEvaluator"/> class.WS
/// </remarks>
public class SqlEvaluator() : ISqlEvaluator
{
    /// <inheritdoc />
    public async Task<EvaluatedQuery> EvaluateAsync(
        string expression,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!expression.Contains("@")) return new EvaluatedQuery(expression);

        var sb = new StringBuilder();
        var parameters = new Dictionary<string, object?>();
        int start = 0;
        int paramIndex = 0;

        while (start < expression.Length)
        {
            int atIndex = expression.IndexOf('@', start);
            if (atIndex == -1)
            {
                sb.Append(expression.Substring(start));
                break;
            }

            sb.Append(expression.Substring(start, atIndex - start));

            int endIndex = atIndex + 1;
            while (endIndex < expression.Length && (char.IsLetterOrDigit((char)expression[endIndex]) || expression[endIndex] == '.' || expression[endIndex] == '_'))
            {
                endIndex++;
            }

            string key = expression.Substring(atIndex + 1, endIndex - atIndex - 1);
            object? value = ResolveValue(key, context);
            if (value is null) throw new NullReferenceException($"No value found for '{key}'.");

            string paramName = $"@param{paramIndex++}";
            parameters[paramName] = value;

            sb.Append(paramName);
            start = endIndex;
        }

        return new EvaluatedQuery(sb.ToString(), parameters);
    }

    private object? ResolveValue(string key, ExpressionExecutionContext context)
    {
        return key switch
        {
            "Workflow.Definition.Id" => context.GetWorkflowExecutionContext().Workflow.Identity.DefinitionId,
            "Workflow.Definition.Version.Id" => context.GetWorkflowExecutionContext().Workflow.Identity.Id,
            "Workflow.Definition.Version" => context.GetWorkflowExecutionContext().Workflow.Identity.Version,
            "Workflow.Instance.Id" => context.GetActivityExecutionContext().WorkflowExecutionContext.Id,
            "Correlation.Id" => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId,
            "LastResult" => context.GetLastResult(),
            var i when i.StartsWith("Input.") => context.GetWorkflowExecutionContext().Input.TryGetValue(i.Substring(6), out var v) ? v : null,
            var o when o.StartsWith("Output.") => context.GetWorkflowExecutionContext().Output.TryGetValue(o.Substring(7), out var v) ? v : null,
            var v when v.StartsWith("Variables.") => context.GetWorkflowExecutionContext().Variables.FirstOrDefault(x => x.Name == v.Substring(10), null)?.Value ?? null,
            _ => null
        };
    }
}