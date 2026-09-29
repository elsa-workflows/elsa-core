using Elsa.Ldap.Options;
using Elsa.Ldap.Services;

namespace Elsa.Ldap.UnitTests.Services;

public class LdapConnectionFactoryTests
{
    [Fact]
    public void ResolveConnection_ThrowsInvalidOperationException_WhenConnectionNotFound()
    {
        // Arrange
        var options = new LdapOptions();
        var factory = new LdapConnectionFactory(Microsoft.Extensions.Options.Options.Create(options));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.ResolveConnection("non-existent"));
    }

    [Fact]
    public void ResolveConnection_ReturnsDefaultConnection_WhenNameIsNull()
    {
        // Arrange
        var defaultConnection = new LdapConnectionOptions { Host = "localhost" };
        var options = new LdapOptions
        {
            Connections = { { "Default", defaultConnection } },
        };
        var factory = new LdapConnectionFactory(Microsoft.Extensions.Options.Options.Create(options));

        // Act
        var result = factory.ResolveConnection(null);

        // Assert
        Assert.Same(defaultConnection, result);
    }

    [Fact]
    public void ResolveConnection_ReturnsNamedConnection_WhenNameIsProvided()
    {
        // Arrange
        var namedConnection = new LdapConnectionOptions { Host = "localhost" };
        var options = new LdapOptions()
        {
            Connections = { { "named", namedConnection } },
        };
        var factory = new LdapConnectionFactory(Microsoft.Extensions.Options.Options.Create(options));

        // Act
        var result = factory.ResolveConnection("named");

        // Assert
        Assert.Same(namedConnection, result);
    }

    [Fact]
    public void CreateConnection_ReturnsConnectionProxy()
    {
        // Arrange
        var connectionOptions = new LdapConnectionOptions { Host = "localhost" };
        var options = new LdapOptions
        {
            Connections = { { "Default", connectionOptions } },
        };
        var factory = new LdapConnectionFactory(Microsoft.Extensions.Options.Options.Create(options));

        // Act
        var connection = factory.CreateConnection();

        // Assert
        Assert.IsType<LdapConnectionProxy>(connection);
    }
}