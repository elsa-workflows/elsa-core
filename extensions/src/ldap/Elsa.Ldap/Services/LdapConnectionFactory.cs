using System.DirectoryServices.Protocols;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Ldap.Services;

internal class LdapConnectionFactory : ILdapConnectionFactory
{
    private readonly LdapOptions _options;

    public LdapConnectionFactory(IOptions<LdapOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public ILdapConnection CreateConnection(string? connectionName = null)
    {
        var connectionOptions = ResolveConnection(connectionName);

        var identifier = new LdapDirectoryIdentifier(connectionOptions.Host, connectionOptions.Port);
        var connection = new LdapConnection(identifier)
        {
            AutoBind = true,
            SessionOptions =
            {
                ReferralChasing = connectionOptions.ReferralChasing,
                SecureSocketLayer = connectionOptions.UseSsl,
            },
        };

        if (connectionOptions.BindDn is not null)
        {
            connection.Credential = new System.Net.NetworkCredential(connectionOptions.BindDn, connectionOptions.BindPassword);
            connection.AuthType = AuthType.Basic;
        }
        else
        {
            connection.AuthType = AuthType.Anonymous;
        }

        return new LdapConnectionProxy(connection);
    }

    /// <summary>
    /// Resolves a named <see cref="LdapConnectionOptions"/> from the configured options.
    /// </summary>
    public LdapConnectionOptions ResolveConnection(string? connectionName)
    {
        var name = connectionName ?? "Default";

        if (!_options.Connections.TryGetValue(name, out var connectionOptions))
        {
            throw new InvalidOperationException(
                $"No LDAP connection named '{name}' was found. " +
                $"Configure connections using UseLdap(options => options.AddConnection(...)).");
        }

        return connectionOptions;
    }
}