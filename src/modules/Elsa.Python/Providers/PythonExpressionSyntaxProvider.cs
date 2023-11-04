using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Python.Expressions;

namespace Elsa.Python.Providers;

internal class PythonExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    private const string SyntaxName = "Python";
    
    public ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var javaScript = CreatePythonDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionSyntaxDescriptor>>(new[] { javaScript });
    }

    private ExpressionSyntaxDescriptor CreatePythonDescriptor() => new()
    {
        Syntax = SyntaxName,
        Type = typeof(PythonExpression),
        CreateExpression = CreateCSharpExpression,
        CreateBlockReference = context => new PythonExpressionBlockReference(context.GetExpression<PythonExpression>()),
        CreateSerializableObject = context => new
        {
            Type = SyntaxName,
            context.GetExpression<PythonExpression>().Value
        }
    };

    private IExpression CreateCSharpExpression(ExpressionConstructorContext context)
    {
        var script = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new PythonExpression(script);
    }
}