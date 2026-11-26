using Elsa.Common.Multitenancy;
using Elsa.Http.Bookmarks;
using Elsa.Http.Options;
using Elsa.Tenants.AspNetCore.Services;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Services;

public class TenantPrefixHttpEndpointRoutesProviderTests
{
    [Fact(DisplayName = "The endpoint of a tenant-agnostic workflow is routed under every tenant's route prefix")]
    public async Task AgnosticTrigger_IsRoutedUnderTenantPrefix()
    {
        var provider = new TenantPrefixHttpEndpointRoutesProvider(Substitute.For<ITenantsProvider>(), Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions()));

        var routes = await provider.GetRoutesAsync(new(new HttpEndpointBookmarkPayload("shared", "get"), Tenant.AgnosticTenantId, CancellationToken.None));

        Assert.Equal(["{tenantPrefix}/workflows/shared", "workflows/shared"], routes.Select(x => x.Route.TrimStart('/')));
    }
}
