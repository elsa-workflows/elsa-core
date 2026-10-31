using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        switch (key)
        {
            case var k when k.StartsWith("Input."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Input.");
                    executionContext.Input.TryGetValue(rootKey, out var root);
                    return ResolveNestedValue(root, nestedPath);
                }
            case var k when k.StartsWith("Output."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Output.");
                    executionContext.Output.TryGetValue(rootKey, out var root);
                    return ResolveNestedValue(root, nestedPath);
                }
            case var k when k.StartsWith("Variable."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Variable.");
                    var root = expressionContext.GetVariableInScope(rootKey);
                    return ResolveNestedValue(root, nestedPath);
                }
            case var k when k.StartsWith("Activity."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Activity.");
                    var root = activityContext;
                    return ResolveNestedValue(root, nestedPath);
                }
            case var k when k.StartsWith("Execution."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Execution.");
                    var root = executionContext;
                    return ResolveNestedValue(root, nestedPath);
                }
            case var k when k.StartsWith("Workflow."):
                {
                    var (rootKey, nestedPath) = GetRootAndPath(k, "Workflow.");
                    var root = executionContext.Workflow;
                    return ResolveNestedValue(root, nestedPath);
                }
            case "LastResult":
                return expressionContext.GetLastResult();
            default:
                throw new NullReferenceException($"No matching property found for {{{{{key}}}}}.");
        }
    }

    /// <summary>
    /// Extracts the root key and the nested path from the specified key, based on the given prefix.
    /// </summary>
    /// <param name="key">The full key from which the root key and nested path are derived. Must start with the specified <paramref
    /// name="prefix"/>.</param>
    /// <param name="prefix">The prefix to remove from the beginning of <paramref name="key"/> to determine the root key and nested path.</param>
    /// <returns>A tuple containing the root key and the nested path: <list type="bullet"> <item><description><c>rootKey</c>: The
    /// portion of the key before the first '.' or '[' after the prefix.</description></item>
    /// <item><description><c>nestedPath</c>: The remaining portion of the key after the root key. Returns an empty
    /// string if no '.' or '[' is found.</description></item> </list></returns>
    private (string rootKey, string nestedPath) GetRootAndPath(string key, string prefix)
    {
        var path = key.Substring(prefix.Length);

        // Find the first '.' or '[' to split rootKey and nestedPath
        var dotIndex = path.IndexOf('.');
        var bracketIndex = path.IndexOf('[');

        int splitIndex;
        if (dotIndex == -1 && bracketIndex == -1)
            return (path, "");
        if (dotIndex == -1)
            splitIndex = bracketIndex;
        else if (bracketIndex == -1)
            splitIndex = dotIndex;
        else
            splitIndex = Math.Min(dotIndex, bracketIndex);

        return (path.Substring(0, splitIndex), path.Substring(splitIndex));
    }

    /// <summary>
    /// Resolves nested property/array paths from an object.
    /// Supports POCOs, ExpandoObject, IDictionary, arrays, lists, and JSON objects.
    /// </summary>
    private object? ResolveNestedValue(object? root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path))
            return root;

        // Split path into segments, handling array indices
        var segments = Regex.Matches(path, @"([^.[]+)|\[(\d+)\]")
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            .ToList();

        object? current = root;
        foreach (var segment in segments)
        {
            if (current == null) throw new NullReferenceException($"No matching property found for {{{{{segment}}}}}.");

            if (int.TryParse(segment, out int idx))
            {
                if (current is Array arr)
                {
                    if (idx < 0 || idx >= arr.Length) throw new IndexOutOfRangeException($"Index {idx} out of range.");
                    current = arr.GetValue(idx);
                }
                else if (current is IList list)
                {
                    if (idx < 0 || idx >= list.Count) throw new IndexOutOfRangeException($"Index {idx} out of range.");
                    current = list[idx];
                }
                else if (current is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Array)
                {
                    if (idx < 0 || idx >= jsonElem.GetArrayLength()) throw new IndexOutOfRangeException($"Index {idx} out of range.");
                    current = jsonElem[idx];
                }
                else
                {
                    throw new NullReferenceException($"No matching array or list found for index [{idx}].");
                }
            }
            else
            {
                if (current is IDictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(segment)) throw new KeyNotFoundException($"Key '{segment}' not found.");
                    current = dict[segment];
                }
                else if (current is JsonElement jsonElem)
                {
                    if (jsonElem.ValueKind == JsonValueKind.Object)
                    {
                        if (!jsonElem.TryGetProperty(segment, out var prop)) throw new KeyNotFoundException($"Property '{segment}' not found.");
                        current = prop;
                    }
                    else
                    {
                        throw new NullReferenceException($"No matching property found for {{{{{segment}}}}}.");
                    }
                }
                else
                {
                    var propInfo = current.GetType().GetProperty(segment);
                    if (propInfo == null) throw new NullReferenceException($"Property '{segment}' not found on type '{current.GetType().Name}'.");
                    current = propInfo.GetValue(current);
                }
            }
        }

        // Unwrap JsonElement, if needed
        if (current is JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.String: return elem.GetString();
                case JsonValueKind.Number: return elem.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Object:
                case JsonValueKind.Array: return elem;
                case JsonValueKind.Null: return null;
            }
        }
        return current;
    }
}