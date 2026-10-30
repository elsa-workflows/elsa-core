using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Contracts;

internal interface ILdapConnectionFactory
{
    /// <summary>
    /// Creates and binds an <see cref="LdapConnection"/> using the <paramref name="connectionName"/> for config retrieval.
    /// </summary>
    public ILdapConnection CreateConnection(string? connectionName = null);
}
