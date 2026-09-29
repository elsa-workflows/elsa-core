using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Labels.Models;
using Refit;

namespace Elsa.Studio.Labels.Client;

/// Represents a client API for managing labels.
public interface ILabelsApi
{
    /// Lists all labels.
    [Get("/labels")]
    Task<ListResponse<Label>> ListAsync(CancellationToken cancellationToken = default);

    /// Gets a label by ID.
    /// <param name="id">The ID of the label to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The label with the specified ID.</returns>
    [Get("/labels/{id}")]
    Task<Label> GetAsync(string id, CancellationToken cancellationToken = default);

    /// Deletes a label by ID.
    /// <param name="id">The ID of the label to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [Delete("/labels/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// Updates a label by ID.
    /// <param name="id">The ID of the label to update.</param>
    /// <param name="model">The updated label data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [Post("/labels/{id}")]
    Task UpdateAsync(string id, LabelInputModel model, CancellationToken cancellationToken = default);

    /// Creates a new label.
    /// <param name="inputModel">The data for the new label.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created label.</returns>
    [Post("/labels")]
    Task<Label> CreateAsync(LabelInputModel? inputModel, CancellationToken cancellationToken = default);
}
