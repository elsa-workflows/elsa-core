using Elsa.Expressions.Models;

namespace Elsa.Expressions.Services;

public interface IExpressionSyntaxProvider
{
    ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}