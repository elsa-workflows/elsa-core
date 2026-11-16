using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Tenants.Mediator;
using Elsa.Tenants.Mediator.Middleware;
using Elsa.Tenants.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Middleware;

public class TenantPropagatingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_EmptyTenantIdHeader_RunsHandlerUnderDefaultTenant()
    {
        // Arrange
        var tenantAccessor = new DefaultTenantAccessor();
        await using var services = new ServiceCollection().BuildServiceProvider();
        var tenantScopeFactory = new DefaultTenantScopeFactory(
            tenantAccessor,
            services.GetRequiredService<IServiceScopeFactory>());
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(string.Empty, Arg.Any<CancellationToken>()).Returns(Tenant.Default);

        string? observedTenantId = null;
        Tenant? observedTenant = null;
        ValueTask Next(CommandContext _)
        {
            observedTenantId = tenantAccessor.TenantId;
            observedTenant = tenantAccessor.Tenant;
            return ValueTask.CompletedTask;
        }

        var middleware = new TenantPropagatingMiddleware(Next, tenantScopeFactory, tenantService, tenantAccessor, Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions()));
        var headers = new Dictionary<object, object>
        {
            [TenantHeaders.TenantIdKey] = string.Empty
        };
        var context = new CommandContext(
            Substitute.For<ICommand>(),
            Substitute.For<ICommandStrategy>(),
            typeof(object),
            headers,
            services,
            CancellationToken.None);
        var ambientTenant = new Tenant { Id = "other-tenant", Name = "Other" };

        // Act
        using (tenantAccessor.PushContext(ambientTenant))
            await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(string.Empty, observedTenantId);
        Assert.Same(Tenant.Default, observedTenant);
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantIdHeader_RunsHandlerByDefault()
    {
        // Arrange
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: false);

        // Act
        await middleware.InvokeAsync(CreateContext(headers: new Dictionary<object, object>()));

        // Assert
        Assert.True(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_UnknownTenantIdHeader_RunsHandlerByDefault()
    {
        // Arrange
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: false);

        // Act
        await middleware.InvokeAsync(CreateContext(TenantHeaders.CreateHeaders("unknown")));

        // Assert
        Assert.True(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantIdHeaderAndRejectingUnresolvedTenants_ThrowsWithoutRunningHandler()
    {
        // Arrange
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(CreateContext(headers: new Dictionary<object, object>())).AsTask());
        Assert.False(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_UnknownTenantIdHeaderAndRejectingUnresolvedTenants_ThrowsWithoutRunningHandler()
    {
        // Arrange
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(CreateContext(TenantHeaders.CreateHeaders("unknown"))).AsTask());
        Assert.False(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_DefaultTenantIdHeaderAndRejectingUnresolvedTenants_RunsHandler()
    {
        // Arrange
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: true);

        // Act
        await middleware.InvokeAsync(CreateContext(TenantHeaders.CreateHeaders(string.Empty)));

        // Assert
        Assert.True(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantIdHeaderInsideTenantScopeAndRejectingUnresolvedTenants_RunsHandler()
    {
        // Arrange
        var tenantAccessor = new DefaultTenantAccessor();
        var (middleware, nextWasCalled) = CreateMiddleware(rejectUnresolvedTenants: true, tenantAccessor);

        // Act
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant1", Name = "Tenant 1" }))
            await middleware.InvokeAsync(CreateContext(headers: new Dictionary<object, object>()));

        // Assert
        Assert.True(nextWasCalled());
    }

    private static (TenantPropagatingMiddleware Middleware, Func<bool> NextWasCalled) CreateMiddleware(bool rejectUnresolvedTenants, ITenantAccessor? tenantAccessor = null)
    {
        tenantAccessor ??= new DefaultTenantAccessor();
        var services = new ServiceCollection().BuildServiceProvider();
        var tenantScopeFactory = new DefaultTenantScopeFactory(tenantAccessor, services.GetRequiredService<IServiceScopeFactory>());
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(string.Empty, Arg.Any<CancellationToken>()).Returns(Tenant.Default);
        var nextWasCalled = false;
        var options = Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions { RejectUnresolvedTenants = rejectUnresolvedTenants });
        var middleware = new TenantPropagatingMiddleware(_ =>
        {
            nextWasCalled = true;
            return ValueTask.CompletedTask;
        }, tenantScopeFactory, tenantService, tenantAccessor, options);

        return (middleware, () => nextWasCalled);
    }

    private static CommandContext CreateContext(IDictionary<object, object> headers) => new(
        Substitute.For<ICommand>(),
        Substitute.For<ICommandStrategy>(),
        typeof(object),
        headers,
        new ServiceCollection().BuildServiceProvider(),
        CancellationToken.None);
}
