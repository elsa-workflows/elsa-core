using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

/// <summary>
/// Generates a unique identifier using <see cref="Guid"/>.
/// </summary>
public class RandomIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId() => Guid.NewGuid().ToString("D");
}