using Elsa.Studio.Secrets.Models;
using Refit;

namespace Elsa.Studio.Secrets.Client;

public interface ISecretsApi
{
    [Get("/secrets")]
    Task<ListSecretsResponse> ListAsync(string? search = null, string? typeName = null, string? storeName = null, string? scope = null, SecretStatus? status = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default);

    [Get("/secrets/{name}")]
    Task<SecretModel> GetAsync(string name, CancellationToken cancellationToken = default);

    [Post("/secrets")]
    Task<SecretModel> CreateAsync([Body] CreateSecretRequest request, CancellationToken cancellationToken = default);

    [Post("/secrets/{name}")]
    Task<SecretModel> UpdateAsync(string name, [Body] UpdateSecretRequest request, CancellationToken cancellationToken = default);

    [Post("/secrets/{name}/rotate")]
    Task<SecretModel> RotateAsync(string name, [Body] RotateSecretRequest request, CancellationToken cancellationToken = default);

    [Post("/secrets/{name}/revoke")]
    Task<SecretModel> RevokeAsync(string name, CancellationToken cancellationToken = default);

    [Delete("/secrets/{name}")]
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);

    [Post("/secrets/{name}/test")]
    Task<SecretTestResponse> TestAsync(string name, CancellationToken cancellationToken = default);

    [Get("/secrets/descriptors")]
    Task<SecretDescriptorsResponse> GetDescriptorsAsync(CancellationToken cancellationToken = default);

    [Post("/secrets/picker")]
    Task<SecretPickerResponse> PickAsync([Body] SecretPickerRequest request, CancellationToken cancellationToken = default);
}
