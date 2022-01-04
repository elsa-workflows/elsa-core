using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface IExpressionSyntaxProvider
{
    ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}