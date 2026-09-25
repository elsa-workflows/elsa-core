namespace Elsa.Ldap.Options;

/// <summary>
/// Top-level configuration options for the LDAP module.
/// Supports multiple named connections to allow targeting different directories.
/// </summary>
public class LdapOptions
{
    /// <summary>
    /// Named LDAP connections. The key is the connection name used in activities.
    /// A connection named <c>Default</c> is used when no connection name is specified.
    /// </summary>
    internal Dictionary<string, LdapConnectionOptions> Connections { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a named LDAP connection.
    /// </summary>
    public LdapOptions AddConnection(string name, LdapConnectionOptions options)
    {
        Connections[name] = options;
        return this;
    }

    /// <summary>
    /// Registers the default LDAP connection (name <c>Default</c>).
    /// </summary>
    public LdapOptions AddDefaultConnection(LdapConnectionOptions options) => AddConnection("Default", options);
}