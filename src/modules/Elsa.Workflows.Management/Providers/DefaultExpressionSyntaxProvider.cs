using Elsa.Expressions;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Expressions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Management.Providers;

public class DefaultExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    private readonly IIdentityGenerator _identityGenerator;

    public DefaultExpressionSyntaxProvider(IIdentityGenerator identityGenerator)
    {
        _identityGenerator = identityGenerator;
    }

    public ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var literal = CreateLiteralDescriptor();
        var json = CreateJsonDescriptor();
        var @delegate = CreateDelegateDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionSyntaxDescriptor>>(new[] { literal, json, @delegate });
    }

    private ExpressionSyntaxDescriptor CreateLiteralDescriptor() => CreateDescriptor<LiteralExpression>(
        "Literal",
        CreateLiteralExpression,
        context => new Literal(context.GetExpression<LiteralExpression>().Value),
        expression => expression.Value);

    private ExpressionSyntaxDescriptor CreateJsonDescriptor() => CreateDescriptor<JsonExpression>(
        "Json",
        CreateJsonExpression,
        context => new JsonObject(context.GetExpression<JsonExpression>().Value),
        expression => expression.Value);

    private ExpressionSyntaxDescriptor CreateDelegateDescriptor() => CreateDescriptor<DelegateExpression>(
        "Delegate",
        CreateJsonExpression,
        context => new DelegateReference(),
        expression => expression.DelegateReference.Delegate?.ToString());

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

    private IExpression CreateLiteralExpression(ExpressionConstructorContext context)
    {
        return !context.Element.TryGetProperty("value", out var expressionValue) 
            ? new LiteralExpression() 
            : new LiteralExpression(expressionValue.ToString());
    }

    private IExpression CreateJsonExpression(ExpressionConstructorContext context)
    {
        var expressionValue = context.Element.GetProperty("value").ToString();
        return new JsonExpression(expressionValue);
    }

    private string GenerateId() => _identityGenerator.GenerateId();
}