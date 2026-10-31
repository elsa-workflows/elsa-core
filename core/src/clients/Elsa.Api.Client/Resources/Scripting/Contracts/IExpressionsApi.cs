using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.Scripting.Contracts;

/// <summary>
/// Represents a client for the expression descriptors API.
/// </summary>
[PublicAPI]
public interface IExpressionDescriptorsApi
{
    /// <summary>
    /// Sends a request to retrieve all available expression descriptors.
    /// </summary>
    [Get("/descriptors/expression-descriptors")]
    Task<ListResponse<ExpressionDescriptor>> ListAsync(CancellationToken cancellationToken);
}