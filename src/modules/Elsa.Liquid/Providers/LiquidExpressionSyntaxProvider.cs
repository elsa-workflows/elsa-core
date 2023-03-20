using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Liquid.Expressions;
using Elsa.Workflows.Core.Contracts;

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
        CreateExpression = CreateLiquidExpression,
        CreateBlockReference = context => new LiquidExpressionBlockReference(context.GetExpression<LiquidExpression>()),
        CreateSerializableObject = context => new
        {
            Type = SyntaxName,
            context.GetExpression<LiquidExpression>().Value
        }
    };

    private IExpression CreateLiquidExpression(ExpressionConstructorContext context)
    {
        var code = context.Element.TryGetProperty("value", out var p) ? p.ToString() : "";
        return new LiquidExpression(code);
    }

    private string GenerateId() => _identityGenerator.GenerateId();
}