using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.AspNetCore.Options;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.UnitTests.Resolvers;

public class HeaderTenantResolverTests
{
    [Theory(DisplayName = "The header resolves a named tenant, but never the \"*\" tenant")]
    [InlineData("tenant-a", true)]
    [InlineData(Tenant.AgnosticTenantId, false)]
    public async Task ResolvesNamedTenantButNotAgnosticTenant(string headerValue, bool expectedResolved)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = headerValue;
        ITenantResolver resolver = new HeaderTenantResolver(new HttpContextAccessor { HttpContext = httpContext }, Microsoft.Extensions.Options.Options.Create(new MultitenancyHttpOptions()));

        var result = await resolver.ResolveAsync(new TenantResolverContext(new Dictionary<string, Tenant>(), CancellationToken.None));

        Assert.Equal(expectedResolved, result.IsResolved);
    }
}
