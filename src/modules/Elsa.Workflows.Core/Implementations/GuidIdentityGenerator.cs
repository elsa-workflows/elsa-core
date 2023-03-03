using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

/// <summary>
/// Generates a unique identifier using <see cref="Guid"/>.
/// </summary>
public class GuidIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId() => Guid.NewGuid().ToString("N");
}