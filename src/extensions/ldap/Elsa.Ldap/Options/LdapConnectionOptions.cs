using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Options;

/// <summary>
/// Configuration options for a single named LDAP connection.
/// </summary>
public class LdapConnectionOptions
{
    /// <summary>
    /// The hostname or IP address of the LDAP server. This property is required.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// The port of the LDAP server. Defaults to 389. The common port for TLS is 636.
    /// </summary>
    public int Port { get; set; } = 389;

    /// <summary>
    /// Whether to use SSL/TLS for the connection. Defaults to false. Use in combination with TLS port.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// The distinguished name to use when binding to the server. Leave null for anonymous bind.
    /// </summary>
    public string? BindDn { get; set; }

    /// <summary>
    /// The password for the bind DN.
    /// </summary>
    public string? BindPassword { get; set; }

    /// <summary>
    /// The referral chasing behavior for the connection.
    /// Defaults to <see cref="ReferralChasingOptions.None"/>.
    /// See <see cref="ReferralChasingOptions"/> for details.
    /// </summary>
    public ReferralChasingOptions ReferralChasing { get; set; } = ReferralChasingOptions.None;
}