using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Middleware;

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task AuthenticatedTenantBindingTakesPrecedenceOverFallback()
    {
        var result = await InvokeBoundAsync("tenant-a", "tenant-a", ["tenant-a"]);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal("tenant-a", result.TenantId);
    }

    [Fact]
    public async Task AuthenticatedDefaultTenantBindingResolves()
    {
        var result = await InvokeBoundAsync(Tenant.DefaultTenantId, Tenant.DefaultTenantId, []);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(Tenant.DefaultTenantId, result.TenantId);
    }

    [Theory]
    [InlineData("tenant-a", null, "tenant-a")]
    [InlineData("tenant-a", "tenant-a", "tenant-b")]
    [InlineData("", "", "tenant-a")]
    public async Task AuthenticatedBindingWithMissingTenantOrContradictoryHintNeverFallsBack(string boundTenantId, string? registeredTenantId, string hint)
    {
        var result = await InvokeBoundAsync(boundTenantId, registeredTenantId, [hint]);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Null(result.TenantId);
    }

    private static async Task<(int StatusCode, string? TenantId)> InvokeBoundAsync(string boundTenantId, string? registeredTenantId, string[] hints)
    {
        var tenants = Substitute.For<ITenantService>();
        if (registeredTenantId is not null)
            tenants.FindAsync(registeredTenantId, Arg.Any<CancellationToken>()).Returns(new Tenant { Id = registeredTenantId, Name = "Registered" });
        var requestHints = Substitute.For<ITenantRequestHints>();
        requestHints.GetAsync(Arg.Any<CancellationToken>()).Returns(hints);
        await using var services = new ServiceCollection().AddSingleton(tenants).AddSingleton(requestHints).BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(TenantPrincipalClaims.BoundTenant, boundTenantId)], "test"))
        };
        context.Response.Body = new MemoryStream();
        var accessor = new DefaultTenantAccessor();
        string? downstreamTenantId = null;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            downstreamTenantId = accessor.Tenant?.Id;
            return Task.CompletedTask;
        }, new DefaultTenantScopeFactory(accessor, services.GetRequiredService<IServiceScopeFactory>()));
        var resolver = Substitute.For<ITenantResolverPipelineInvoker>();
        resolver.InvokePipelineAsync(Arg.Any<CancellationToken>()).Returns(Tenant.Default);

        await middleware.InvokeAsync(context, resolver);

        await resolver.DidNotReceiveWithAnyArgs().InvokePipelineAsync();
        return (context.Response.StatusCode, downstreamTenantId);
    }

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
