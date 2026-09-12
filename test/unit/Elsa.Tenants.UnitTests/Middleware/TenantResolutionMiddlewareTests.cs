using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Middleware;

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextThrows_RestoresOriginalRequestServices()
    {
        await using var rootProvider = new ServiceCollection()
            .AddScoped(_ => new ScopedProbe())
            .BuildServiceProvider();
        await using var originalRequestScope = rootProvider.CreateAsyncScope();
        var originalRequestServices = originalRequestScope.ServiceProvider;
        var context = new DefaultHttpContext { RequestServices = originalRequestServices };
        var expectedException = new InvalidOperationException("Downstream failure");
        var tenantScopeFactory = new DefaultTenantScopeFactory(
            new DefaultTenantAccessor(),
            rootProvider.GetRequiredService<IServiceScopeFactory>());
        var middleware = new TenantResolutionMiddleware(
            _ => Task.FromException(expectedException),
            tenantScopeFactory);
        var tenantResolverPipelineInvoker = Substitute.For<ITenantResolverPipelineInvoker>();
        tenantResolverPipelineInvoker
            .InvokePipelineAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(null));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, tenantResolverPipelineInvoker));

        Assert.Same(expectedException, exception);
        Assert.Same(originalRequestServices, context.RequestServices);
        Assert.NotNull(context.RequestServices.GetRequiredService<ScopedProbe>());
    }

    private sealed class ScopedProbe
    {
    }
}
