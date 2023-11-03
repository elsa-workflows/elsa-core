using Elsa.CSharp.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.CSharp.Providers;

internal class CSharpExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    private const string SyntaxName = "CSharp";
    
    public ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var javaScript = CreateCSharpDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionSyntaxDescriptor>>(new[] { javaScript });
    }

    private ExpressionSyntaxDescriptor CreateCSharpDescriptor() => new()
    {
        Syntax = SyntaxName,
        Type = typeof(CSharpExpression),
        CreateExpression = CreateCSharpExpression,
        CreateBlockReference = context => new CSharpExpressionBlockReference(context.GetExpression<CSharpExpression>()),
        CreateSerializableObject = context => new
        {
            Type = SyntaxName,
            context.GetExpression<CSharpExpression>().Value
        }
    };

    private IExpression CreateCSharpExpression(ExpressionConstructorContext context)
    {
        var script = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new CSharpExpression(script);
    }
}