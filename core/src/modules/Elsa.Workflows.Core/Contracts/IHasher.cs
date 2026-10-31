namespace Elsa.Workflows;

/// <summary>
/// Computes a hash for a given value.
/// </summary>
public interface IHasher
{
    /// <summary>
    /// Produces a hash from the specified string.
    /// </summary>
    string Hash(string value);

    /// <summary>
    /// Produces a hash from the specified values.
    /// </summary>
    string Hash(params object?[] values);
}