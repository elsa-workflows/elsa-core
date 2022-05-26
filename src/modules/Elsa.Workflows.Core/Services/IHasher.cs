namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Computes a hash for a given value.
/// </summary>
public interface IHasher
{
    public string Hash(object value);
    public string Hash(string value);
}