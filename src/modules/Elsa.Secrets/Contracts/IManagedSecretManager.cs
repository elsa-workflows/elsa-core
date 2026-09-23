using Elsa.Secrets.Models;

namespace Elsa.Secrets.Contracts;

/// <summary>
/// Trusted host-only access to immutable lifecycle-owned secret generations.
/// Callers must authorize the connection operation before resolving a value and must prove a generation is
/// unreferenced before deleting it. This is not an HTTP-facing authorization boundary.
/// </summary>
public interface IManagedSecretManager
{
    Task<Secret> CreateGenerationAsync(string ownerId, string generationId, string value, CancellationToken cancellationToken = default);

    Task<SecretPayload> ResolveGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the encrypted payload after the lifecycle owner has atomically proved that the generation is not
    /// current and is not referenced by an unresolved operation. This method validates ownership only.
    /// </summary>
    Task<bool> DeleteGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default);
}
