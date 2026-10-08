using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.Authentication.Abstractions.Options;

/// <summary>
/// Configures the one authentication provider active for an Elsa Studio host.
/// </summary>
public sealed class StudioAuthenticationOptions
{
    public StudioAuthenticationProvider Provider { get; set; }
}
