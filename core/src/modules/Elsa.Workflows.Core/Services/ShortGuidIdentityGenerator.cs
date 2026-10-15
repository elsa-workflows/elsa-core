using DEDrake;

namespace Elsa.Workflows;

/// <summary>
/// Generates a unique identifier using <see cref="ShortGuid"/>.
/// </summary>
public class ShortGuidIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId() => ShortGuid.NewGuid().ToString();
}