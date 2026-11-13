using Elsa.Actors.ProtoActor;
using Elsa.Actors.ProtoActor.Middleware;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Proto;
using Proto.DependencyInjection;

namespace Elsa.Actors.ProtoActor.UnitTests.Middleware;

public class TenantScopeMiddlewareTests
{
    private readonly IServiceProvider _servicesWithoutTenancy = new ServiceCollection().BuildServiceProvider();
    private readonly MessageEnvelope _envelope = new("message", null, MessageHeader.Empty.With(HeaderNames.TenantId, "tenant-1"));

    [Fact(DisplayName = "Sending through a host without multitenancy passes the message on")]
    public async Task PropagateTenant_WithoutTenancy_CallsNextSender()
    {
        var forwarded = false;
        Sender next = (_, _, _) => { forwarded = true; return Task.CompletedTask; };

        await next.PropagateTenant(_servicesWithoutTenancy)(Substitute.For<ISenderContext>(), PID.FromAddress("local", "actor"), _envelope);

        Assert.True(forwarded);
    }

    [Fact(DisplayName = "Sending within a tenant scope forwards the tenant header")]
    public async Task PropagateTenant_WithTenant_ForwardsTenantHeader()
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.Tenant.Returns(new Tenant { Id = "tenant-1", Name = "Tenant 1" });
        var services = new ServiceCollection().AddSingleton(tenantAccessor).BuildServiceProvider();

        MessageEnvelope? forwarded = null;
        Sender next = (_, _, envelope) => { forwarded = envelope; return Task.CompletedTask; };

        await next.PropagateTenant(services)(Substitute.For<ISenderContext>(), PID.FromAddress("local", "actor"), new MessageEnvelope("message", null));

        Assert.NotNull(forwarded);
        Assert.Equal("tenant-1", forwarded!.Header.GetValueOrDefault(HeaderNames.TenantId));
    }

    [Fact(DisplayName = "Receiving a tenant header in a host without multitenancy passes the message on")]
    public async Task ReadTenant_WithoutTenancy_CallsNextReceiver()
    {
        var forwarded = false;
        Receiver next = (_, _) => { forwarded = true; return Task.CompletedTask; };

        await next.ReadTenant(_servicesWithoutTenancy)(Substitute.For<IReceiverContext>(), _envelope);

        Assert.True(forwarded);
    }

    [Fact(DisplayName = "Receiving restores the original service provider when forwarding fails")]
    public async Task ReadTenant_WhenForwardingThrows_RestoresOriginalServiceProvider()
    {
        var tenant = new Tenant { Id = "tenant-1", Name = "Tenant 1" };
        var originalActor = new MarkerActor();
        var tenantScope = CreateTenantScope(tenant, new MarkerActor());

        var tenantFinder = Substitute.For<ITenantFinder>();
        tenantFinder.FindByIdAsync(tenant.Id).Returns(tenant);
        var tenantScopeFactory = Substitute.For<ITenantScopeFactory>();
        tenantScopeFactory.CreateScope(tenant).Returns(tenantScope);
        var services = new ServiceCollection()
            .AddSingleton(tenantFinder)
            .AddSingleton(tenantScopeFactory)
            .AddSingleton(originalActor)
            .BuildServiceProvider();

        await using var system = new ActorSystem().WithServiceProvider(services);
        var context = Substitute.For<IReceiverContext>();
        context.System.Returns(system);

        Receiver next = (_, _) => throw new InvalidOperationException("Forwarding failed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => next.ReadTenant(services)(context, _envelope));

        var producer = system.DI().PropsFor<MarkerActor>().Producer;
        Assert.Same(originalActor, producer(system, null!));
    }

    private static TenantScope CreateTenantScope(Tenant tenant, MarkerActor actor)
    {
        var tenantServices = new ServiceCollection().AddSingleton(actor).BuildServiceProvider();
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(tenantServices);
        return new TenantScope(serviceScope, Substitute.For<ITenantAccessor>(), tenant);
    }

    private sealed class MarkerActor : IActor
    {
        public Task ReceiveAsync(IContext context) => Task.CompletedTask;
    }
}
