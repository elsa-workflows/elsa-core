namespace Elsa.ExternalAuthentication.Options;

/// <summary>
/// Identifies a deployment-installed External Authentication extension without
/// requiring its runtime service to be instantiated during options validation.
/// </summary>
public sealed record ExternalAuthenticationExtensionRegistration(
    ExternalAuthenticationExtensionKind Kind,
    string Type);

/// <summary>Identifies the extension registry populated by a deployment registration.</summary>
public enum ExternalAuthenticationExtensionKind
{
    /// <summary>A provider protocol adapter.</summary>
    Adapter,
    /// <summary>An unlinked external identity admission policy.</summary>
    UnlinkedIdentityPolicy,
    /// <summary>An Elsa permission grant source.</summary>
    PermissionGrantSource
}

/// <summary>
/// Contains the deployment-installed External Authentication extension catalog.
/// </summary>
public sealed class ExternalAuthenticationExtensionOptions
{
    /// <summary>Startup-installed extensions used for duplicate and allowlist validation.</summary>
    public ICollection<ExternalAuthenticationExtensionRegistration> Registrations { get; } = [];
}
