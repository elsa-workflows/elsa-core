using Elsa.Management.Models;

namespace Elsa.Management.Services;

public interface IExpressionSyntaxProvider
{
    ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}