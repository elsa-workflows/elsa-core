using Elsa.Api.Client.Shared.Models;
using Elsa.Secrets;
using Elsa.Secrets.BulkActions;
using Elsa.Secrets.UniqueName;
using Refit;

namespace Elsa.Studio.Secrets.Client;

///  Represents a client API for managing secrets.
public interface ISecretsApi
{
    /// Lists all secrets.
    [Get("/secrets")]
    Task<ListResponse<SecretModel>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Gets a secret by ID.
    [Get("/secrets/{id}")]
    Task<SecretModel> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Gets a secret input model by ID.
    [Get("/secrets/{id}/input")]
    Task<SecretInputModel> GetInputAsync(string id, CancellationToken cancellationToken = default);
    
    /// Generates a unique name for a secret.
    [Post("/actions/secrets/generate-unique-name")]
    Task<GenerateUniqueNameResponse> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    
    /// Checks if a name is unique for a secret.
    [Post("/queries/secrets/is-unique-name")]
    Task<IsUniqueNameResponse> GetIsNameUniqueAsync(IsUniqueNameRequest request, CancellationToken cancellationToken);
    
    /// Creates a new secret.
    [Post("/secrets")]
    Task<SecretModel> CreateAsync(SecretInputModel request, CancellationToken cancellationToken = default);
    
    /// Updates a secret.
    [Post("/secrets/{id}")]
    Task<SecretModel> UpdateAsync(string id, SecretInputModel request, CancellationToken cancellationToken = default);

    /// Deletes a secret.
    [Delete("/secrets/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// Deletes multiple secrets.
    [Post("/bulk-actions/secrets/delete")]
    Task<BulkDeleteResponse> BulkDeleteAsync(BulkDeleteRequest request, CancellationToken cancellationToken = default);
}