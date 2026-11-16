using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantScopeTests
{
    [Fact(DisplayName = "Disposing a tenant scope restores the caller's tenant")]
    public async Task DisposeAsyncRestoresCallerTenant()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        await using var services = new ServiceCollection().BuildServiceProvider();
        var tenantScopeFactory = new DefaultTenantScopeFactory(tenantAccessor, services.GetRequiredService<IServiceScopeFactory>());
        var callerTenant = new Tenant { Id = "caller", Name = "caller" };
        using var callerContext = tenantAccessor.PushContext(callerTenant);

        await using (tenantScopeFactory.CreateScope(new Tenant { Id = "scoped", Name = "scoped" }))
            Assert.Equal("scoped", tenantAccessor.TenantId);

        Assert.Same(callerTenant, tenantAccessor.Tenant);
    }
}
