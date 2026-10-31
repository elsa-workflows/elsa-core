namespace Elsa.Identity.Contracts;

/// <summary>
/// Generates secrets.
/// </summary>
public interface ISecretGenerator
{
    /// <summary>
    /// Generates a secret.
    /// </summary>
    /// <param name="length">The length of the secret.</param>
    /// <returns>The secret.</returns>
    string Generate(int length = 32);
}