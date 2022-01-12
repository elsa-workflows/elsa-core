using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;
using Elsa.Scripting.Liquid.Expressions;

namespace Elsa.Scripting.Liquid.Providers;

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
            var reference = new LiquidExpressionReference(context.GetExpression<LiquidExpression>());

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