using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides syntax services.
/// </summary>
public interface IExpressionService
{
    /// <summary>
    /// Lists all expression descriptors.
    /// </summary>
    Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a syntax provider by name.
    /// </summary>
    Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
}