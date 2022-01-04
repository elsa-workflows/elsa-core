namespace Elsa.Contracts;

/// <summary>
/// Computes a hash for a given value.
/// </summary>
public interface IHasher
{
    public string Hash(object value);
}