using Elsa.Api.Client.Resources.StorageDrivers.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.StorageDrivers.Contracts;

/// <summary>
/// Represents a client for the storage drivers API.
/// </summary>
public interface IStorageDriversApi
{
    /// <summary>
    /// Lists storage drivers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the storage drivers.</returns>
    [Get("/descriptors/storage-drivers")]
    Task<ListStorageDriversResponse> ListAsync(CancellationToken cancellationToken = default);
}