using Elsa.Actors.ProtoActor;
using Elsa.Actors.ProtoActor.Middleware;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Proto;
using Proto.DependencyInjection;

namespace Elsa.Actors.ProtoActor.UnitTests.Middleware;

public class TenantScopeConcurrencyTests : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly Tenant _tenant1 = new() { Id = "tenant-1", Name = "Tenant 1" };
    private readonly Tenant _tenant2 = new() { Id = "tenant-2", Name = "Tenant 2" };
    private readonly ITenantAccessor _tenantAccessor = new DefaultTenantAccessor();
    private readonly ActorSystem _system;
    private readonly IServiceProvider _rootServices;

    public TenantScopeConcurrencyTests()
    {
        var tenantFinder = Substitute.For<ITenantFinder>();
        tenantFinder.FindByIdAsync(_tenant1.Id).Returns(_tenant1);
        tenantFinder.FindByIdAsync(_tenant2.Id).Returns(_tenant2);

        // Each tenant scope has its own provider, so replacing the actor system's provider with it would be visible below.
        var tenantScopeFactory = Substitute.For<ITenantScopeFactory>();
        tenantScopeFactory.CreateScope(Arg.Any<Tenant?>()).Returns(call => CreateTenantScope(call.Arg<Tenant>()));

        _rootServices = new ServiceCollection()
            .AddSingleton(tenantFinder)
            .AddSingleton(tenantScopeFactory)
            .AddSingleton(new MarkerActor("root"))
            .BuildServiceProvider();
        _system = new ActorSystem().WithServiceProvider(_rootServices);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _system.DisposeAsync();

    [Fact(DisplayName = "Overlapping messages for different tenants each see their own tenant and never replace the actor system's service provider")]
    public async Task ReadTenant_OverlappingTenants_KeepOwnTenantAndRootServiceProvider()
    {
        // Both messages must be inside ReadTenant before either continues, so the receivers are guaranteed to overlap.
        var bothReceiving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiving = 0;

        var props = Props.FromFunc(async context =>
        {
            if (context.Message is not "report")
                return;

            if (Interlocked.Increment(ref receiving) == 2)
                bothReceiving.SetResult();

            await bothReceiving.Task.WaitAsync(Timeout);

            context.Respond((_tenantAccessor.Tenant?.Id, ResolveActorSystemMarker()));
        }).WithMultitenancy(_rootServices);

        var receiver1 = _system.Root.Spawn(props);
        var receiver2 = _system.Root.Spawn(props);

        var result1 = Report(receiver1, _tenant1.Id);
        var result2 = Report(receiver2, _tenant2.Id);

        Assert.Equal((_tenant1.Id, "root"), await result1);
        Assert.Equal((_tenant2.Id, "root"), await result2);
        Assert.Equal("root", ResolveActorSystemMarker());
        Assert.Null(_tenantAccessor.Tenant);
    }

    private Task<(string?, string)> Report(PID receiver, string tenantId) =>
        _system.Root.RequestAsync<(string?, string)>(receiver, new MessageEnvelope("report", null, MessageHeader.Empty.With(HeaderNames.TenantId, tenantId)), Timeout);

    // The producer of DependencyResolver's props only resolves from the provider it was created with and ignores its context, so null is fine here.
    private string ResolveActorSystemMarker() => ((MarkerActor)_system.DI().PropsFor<MarkerActor>().Producer(_system, null!)).Name;

    private TenantScope CreateTenantScope(Tenant tenant)
    {
        var tenantServices = new ServiceCollection().AddSingleton(new MarkerActor(tenant.Id)).BuildServiceProvider();
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(tenantServices);
        return new TenantScope(serviceScope, _tenantAccessor, tenant);
    }

    private sealed class MarkerActor(string name) : IActor
    {
        public string Name { get; } = name;
        public Task ReceiveAsync(IContext context) => Task.CompletedTask;
    }
}
