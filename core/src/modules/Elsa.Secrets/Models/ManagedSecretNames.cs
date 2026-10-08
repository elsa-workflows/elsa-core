namespace Elsa.Secrets.Models;

/// <summary>Deterministic opaque names make retries for the same owner/generation collide safely at the Secrets store.</summary>
public static class ManagedSecretNames
{
    public static string ForGeneration(string ownerId, string generationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(generationId);

        var identity = System.Text.Encoding.UTF8.GetBytes($"{ownerId}\0{generationId}");
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(identity)).ToLowerInvariant();
        return $"connection:credential:{digest}";
    }
}
