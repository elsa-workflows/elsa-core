namespace Elsa.Workflows.Contracts;

/// <summary>
/// Represents a unique identity generator.
/// </summary>
public interface IIdentityGenerator
{
    /// <summary>
    /// Generates a unique identifier.
    /// </summary>
    string GenerateId();
}