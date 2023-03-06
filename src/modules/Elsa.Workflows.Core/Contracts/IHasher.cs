namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Computes a hash for a given value.
/// </summary>
public interface IHasher
{
    /// <summary>
    /// Produces a hash from the specified string.
    /// </summary>
    public string Hash(string value);
}