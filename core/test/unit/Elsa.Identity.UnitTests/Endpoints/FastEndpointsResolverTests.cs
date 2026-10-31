using Elsa.Identity.Contracts;
using Elsa.Identity.Endpoints.Users.Create;
using Elsa.UnitTests.Shared;
using FastEndpoints;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// Pins the cleanup the classes in <see cref="FastEndpointsCollection"/> depend on, in the one order that used to fail:
/// an endpoint host runs and is disposed, and only then is an endpoint created through the factory.
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class FastEndpointsResolverTests
{
    [Fact]
    public async Task FactoryCreatesEndpointsAfterAnEndpointHostIsDisposed()
    {
        var host = new LogoutEndpointTests();
        await host.InitializeAsync();
        await host.DisposeAsync();

        Assert.NotNull(Factory.Create<Create>(Substitute.For<IUserManager>(), Substitute.For<IRoleAuthorizationService>()));
    }
}
