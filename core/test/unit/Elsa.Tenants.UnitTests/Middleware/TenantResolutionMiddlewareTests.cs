using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Elsa.Tenants.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            () => middleware.InvokeAsync(context, tenantResolverPipelineInvoker, Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions())));

        Assert.Same(expectedException, exception);
        Assert.Same(originalRequestServices, context.RequestServices);
        Assert.NotNull(context.RequestServices.GetRequiredService<ScopedProbe>());
    }

    [Fact]
    public async Task InvokeAsync_WhenNoTenantResolves_ContinuesByDefault()
    {
        // Arrange
        var (context, nextWasCalled, middleware, invoker) = CreateMiddleware(resolvedTenant: null);

        // Act
        await middleware.InvokeAsync(context, invoker, Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions()));

        // Assert
        Assert.True(nextWasCalled());
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoTenantResolvesAndRejectingUnresolvedTenants_RespondsBadRequestWithoutContinuing()
    {
        // Arrange
        var (context, nextWasCalled, middleware, invoker) = CreateMiddleware(resolvedTenant: null);

        // Act
        await middleware.InvokeAsync(context, invoker, Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions { RejectUnresolvedTenants = true }));

        // Assert
        Assert.False(nextWasCalled());
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenTenantResolvesAndRejectingUnresolvedTenants_Continues()
    {
        // Arrange
        var (context, nextWasCalled, middleware, invoker) = CreateMiddleware(resolvedTenant: Tenant.Default);

        // Act
        await middleware.InvokeAsync(context, invoker, Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions { RejectUnresolvedTenants = true }));

        // Assert
        Assert.True(nextWasCalled());
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static (HttpContext Context, Func<bool> NextWasCalled, TenantResolutionMiddleware Middleware, ITenantResolverPipelineInvoker Invoker) CreateMiddleware(Tenant? resolvedTenant)
    {
        var rootProvider = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = rootProvider };
        var nextWasCalled = false;
        var tenantScopeFactory = new DefaultTenantScopeFactory(new DefaultTenantAccessor(), rootProvider.GetRequiredService<IServiceScopeFactory>());
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        }, tenantScopeFactory);
        var invoker = Substitute.For<ITenantResolverPipelineInvoker>();
        invoker.InvokePipelineAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(resolvedTenant));

        return (context, () => nextWasCalled, middleware, invoker);
    }

    private sealed class ScopedProbe
    {
    }
}
