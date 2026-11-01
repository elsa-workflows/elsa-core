using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Tenants.Mediator;
using Elsa.Tenants.Mediator.Middleware;
using Microsoft.Extensions.DependencyInjection;
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

        var middleware = new TenantPropagatingMiddleware(Next, tenantScopeFactory, tenantService);
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
}
