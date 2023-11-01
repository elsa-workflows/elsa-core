using Elsa.CSharp.Contracts;
using Elsa.Expressions.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Elsa.CSharp.Services;

/// <summary>
/// A c# expression evaluator using Roslyn.
/// </summary>
public class RoslynCSharpEvaluator : ICSharpEvaluator
{
    /// <inheritdoc />
    public async Task<object?> EvaluateAsync(string expression, Type returnType, ExpressionExecutionContext context, CancellationToken cancellationToken = default)
    {
        var result = await CSharpScript.EvaluateAsync(expression, cancellationToken: cancellationToken);
        return result;
    }
}