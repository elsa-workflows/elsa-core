using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides available expression descriptors.
/// </summary>
public interface IExpressionProvider
{
    /// <summary>
    /// Lists all expression descriptors.
    /// </summary>
    ValueTask<IEnumerable<ExpressionDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}