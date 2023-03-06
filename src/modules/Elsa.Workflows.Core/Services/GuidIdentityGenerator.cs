using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Generates a unique identifier using <see cref="Guid"/>.
/// </summary>
public class GuidIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId() => Guid.NewGuid().ToString("N");
}