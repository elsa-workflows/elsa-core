using Elsa.Authorization;
using Elsa.Testing.Shared.Authorization;

namespace Elsa.Api.Common.UnitTests.Abstractions;

[Collection(nameof(EndpointSecurityCollection))]
public class AuthorizationTestHostTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Disposing_RestoresTheSecuritySettingFromBeforeTheHostStarted(bool securityWasEnabled)
    {
        var original = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = securityWasEnabled;

        try
        {
            var host = await AuthorizationTestHost.StartAsync<RequireAnyPermissionTests.AnyOfEndpoint>(endpoint => endpoint == typeof(RequireAnyPermissionTests.AnyOfEndpoint));
            Assert.True(EndpointSecurityOptions.SecurityIsEnabled);

            await host.DisposeAsync();

            Assert.Equal(securityWasEnabled, EndpointSecurityOptions.SecurityIsEnabled);
        }
        finally
        {
            EndpointSecurityOptions.SecurityIsEnabled = original;
        }
    }
}
