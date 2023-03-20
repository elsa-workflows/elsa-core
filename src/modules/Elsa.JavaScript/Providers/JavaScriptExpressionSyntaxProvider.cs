using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.JavaScript.Expressions;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.JavaScript.Providers;

internal class JavaScriptExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    private const string SyntaxName = "JavaScript";
    private readonly IIdentityGenerator _identityGenerator;

    public JavaScriptExpressionSyntaxProvider(IIdentityGenerator identityGenerator)
    {
        _identityGenerator = identityGenerator;
    }

    public ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var javaScript = CreateJavaScriptDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionSyntaxDescriptor>>(new[] { javaScript });
    }

    private ExpressionSyntaxDescriptor CreateJavaScriptDescriptor() => new()
    {
        Syntax = SyntaxName,
        Type = typeof(JavaScriptExpression),
        CreateExpression = CreateJavaScriptExpression,
        CreateBlockReference = context => new JavaScriptExpressionBlockReference(context.GetExpression<JavaScriptExpression>()),
        CreateSerializableObject = context => new
        {
            Type = SyntaxName,
            context.GetExpression<JavaScriptExpression>().Value
        }
    };

    private IExpression CreateJavaScriptExpression(ExpressionConstructorContext context)
    {
        var script = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new JavaScriptExpression(script);
    }

    private string GenerateId() => _identityGenerator.GenerateId();
}