using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Liquid.Expressions;
using Elsa.Workflows.Core.Services;

namespace Elsa.Liquid.Providers;

public class LiquidExpressionSyntaxProvider : IExpressionSyntaxProvider
{
    public const string SyntaxName = "Liquid";
    private readonly IIdentityGenerator _identityGenerator;
    public LiquidExpressionSyntaxProvider(IIdentityGenerator identityGenerator) => _identityGenerator = identityGenerator;

    public ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var liquidDescriptor = CreateLiquidDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionSyntaxDescriptor>>(new[] { liquidDescriptor });
    }

    private ExpressionSyntaxDescriptor CreateLiquidDescriptor() => new()
    {
        Syntax = SyntaxName,
        Type = typeof(LiquidExpression),
        CreateExpression = CreateJavaScriptExpression,
        CreateLocationReference = context =>
        {
            var reference = new LiquidExpressionBlockReference(context.GetExpression<LiquidExpression>());

            if (string.IsNullOrWhiteSpace(reference.Id))
                reference.Id = GenerateId();

            return reference;
        },
        CreateSerializableObject = context => new
        {
            Type = SyntaxName,
            context.GetExpression<LiquidExpression>().Value
        }
    };

    private IExpression CreateJavaScriptExpression(ExpressionConstructorContext context)
    {
        var script = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new LiquidExpression(script);
    }

    private string GenerateId() => _identityGenerator.GenerateId();
}