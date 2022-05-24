using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

public interface IExpressionSyntaxProvider
{
    ValueTask<IEnumerable<ExpressionSyntaxDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}