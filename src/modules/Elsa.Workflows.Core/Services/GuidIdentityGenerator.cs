using Elsa.Workflows.Contracts;

namespace Elsa.Workflows;

/// <summary>
/// Generates a unique identifier using <see cref="Guid"/>.
/// </summary>
public class GuidIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId() => Guid.NewGuid().ToString("N");
}