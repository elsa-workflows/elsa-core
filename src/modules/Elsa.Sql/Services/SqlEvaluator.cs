using System.Text;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Sql.Contracts;
using Elsa.Sql.Models;
using Elsa.Workflows;

namespace Elsa.Sql.Services;

/// <summary>
/// A SQL expression evaluator.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SqlEvaluator"/> class.WS
/// </remarks>
public class SqlEvaluator() : ISqlEvaluator
{
    private WorkflowExecutionContext executionContext;
    private ActivityExecutionContext activityContext;
    private ExpressionExecutionContext expressionContext;

    /// <inheritdoc />
    public async Task<EvaluatedQuery> EvaluateAsync(
        string expression,
        ExpressionExecutionContext context,
        ExpressionEvaluatorOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!expression.Contains("{{")) return new EvaluatedQuery(expression);

        expressionContext = context;
        executionContext = context.GetWorkflowExecutionContext();
        activityContext = context.GetActivityExecutionContext();

        // Create client
        var factory = context.GetRequiredService<ISqlClientFactory>();
        var client = factory.CreateClient(activityContext.ActivityState["Client"].ToString(), activityContext.ActivityState["ConnectionString"].ToString());

        var sb = new StringBuilder();
        int start = 0;
        var parameters = new Dictionary<string, object?>();
        int paramIndex = 0;

        while (start < expression.Length)
        {
            int openIndex = expression.IndexOf("{{", start);
            if (openIndex == -1)
            {
                sb.Append(expression.AsSpan(start));
                break;
            }

            // Append everything before {{
            sb.Append(expression.AsSpan(start, openIndex - start));

            // Find the closing }}
            int closeIndex = expression.IndexOf("}}", openIndex + 2);
            if (closeIndex == -1) throw new FormatException("Unmatched '{{' found in SQL expression.");

            // Extract key
            string key = expression.Substring(openIndex + 2, closeIndex - openIndex - 2).Trim();
            if (string.IsNullOrEmpty(key)) throw new FormatException("Empty placeholder '{{}}' is not allowed.");

            // Resolve value and replace with parameterized name
            var counterValue = client.IncrementParameter ? paramIndex++.ToString() : string.Empty;
            string paramName = $"{client.ParameterMarker}{client.ParameterText}{counterValue}";
            parameters[paramName] = ResolveValue(key);

            sb.Append(paramName);
            start = closeIndex + 2;
        }

        return new EvaluatedQuery(sb.ToString(), parameters);
    }

    private object? ResolveValue(string key)
    {
        return key switch
        {
            "Workflow.Definition.Id" => executionContext.Workflow.Identity.DefinitionId,
            "Workflow.Definition.Version.Id" => executionContext.Workflow.Identity.Id,
            "Workflow.Definition.Version" => executionContext.Workflow.Identity.Version,
            "Workflow.Instance.Id" => activityContext.WorkflowExecutionContext.Id,
            "Correlation.Id" => activityContext.WorkflowExecutionContext.CorrelationId,
            "LastResult" => expressionContext.GetLastResult(),
            var i when i.StartsWith("Input.") => executionContext.Input.TryGetValue(i.Substring(6), out var v) ? v : null,
            var o when o.StartsWith("Output.") => executionContext.Output.TryGetValue(o.Substring(7), out var v) ? v : null,
            var v when v.StartsWith("Variables.") => executionContext.Variables.FirstOrDefault(x => x.Name == v.Substring(10), null)?.Value ?? null,
            _ => throw new NullReferenceException($"No matching property found for {{{{{key}}}}}.")
        };
    }
}