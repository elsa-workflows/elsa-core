using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Secrets.Expressions;

/// <summary>
/// Resolves Secret expressions through the configured secret resolver.
/// </summary>
public class SecretExpressionHandler(ISecretResolver secretResolver, IWellKnownTypeRegistry wellKnownTypeRegistry) : IExpressionHandler
{
    /// <inheritdoc />
    public async ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options)
    {
        if (expression.Value is not SecretReference reference)
            throw new InvalidOperationException("Secret expression value must be a SecretReference.");

        if (string.IsNullOrWhiteSpace(reference.Name))
            throw new InvalidOperationException("Secret expression reference must specify a secret name.");

        var value = await secretResolver.ResolveAsync(reference, context.CancellationToken);
        return value.ConvertTo(returnType, new ObjectConverterOptions(WellKnownTypeRegistry: wellKnownTypeRegistry));
    }
}
