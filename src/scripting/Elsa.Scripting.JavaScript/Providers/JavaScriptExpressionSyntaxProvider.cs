using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Expressions;

namespace Elsa.Scripting.JavaScript.Providers;

public class JavaScriptExpressionSyntaxProvider : IExpressionSyntaxProvider
{
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

    private ExpressionSyntaxDescriptor CreateJavaScriptDescriptor() => CreateDescriptor<JavaScriptExpression>(
        "JavaScript",
        CreateJavaScriptExpression,
        context => new JavaScriptExpressionReference(context.GetExpression<JavaScriptExpression>()),
        expression => expression.Value);
    
    private ExpressionSyntaxDescriptor CreateDescriptor<TExpression>(
        string syntax,
        Func<ExpressionConstructorContext, IExpression> constructor,
        Func<LocationReferenceConstructorContext, RegisterLocationReference> createLocationReference,
        Func<TExpression, object?> expressionValue) =>
        new()
        {
            Syntax = syntax,
            Type = typeof(TExpression),
            CreateExpression = constructor,
            CreateLocationReference = context =>
            {
                var reference = createLocationReference(context);

                if (string.IsNullOrWhiteSpace(reference.Id))
                    reference.Id = GenerateId();

                return reference;
            },
            CreateSerializableObject = context => new
            {
                Type = syntax,
                Value = expressionValue(context.GetExpression<TExpression>())
            }
        };

    private IExpression CreateJavaScriptExpression(ExpressionConstructorContext context)
    {
        var script = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new JavaScriptExpression(script);
    }
    
    private string GenerateId() => _identityGenerator.GenerateId();
}