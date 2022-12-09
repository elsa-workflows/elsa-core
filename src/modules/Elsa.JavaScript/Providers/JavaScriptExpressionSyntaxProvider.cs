using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.JavaScript.Expressions;
using Elsa.Workflows.Core.Services;

namespace Elsa.JavaScript.Providers;

public class JavaScriptExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    public const string SyntaxName = "JavaScript";
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