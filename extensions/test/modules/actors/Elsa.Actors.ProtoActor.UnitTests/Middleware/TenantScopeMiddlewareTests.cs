using Elsa.Actors.ProtoActor;
using Elsa.Actors.ProtoActor.Middleware;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Proto;

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

    [Fact(DisplayName = "Receiving a tenant header in a host without multitenancy passes the message on")]
    public async Task ReadTenant_WithoutTenancy_CallsNextReceiver()
    {
        var forwarded = false;
        Receiver next = (_, _) => { forwarded = true; return Task.CompletedTask; };

        await next.ReadTenant(_servicesWithoutTenancy)(Substitute.For<IReceiverContext>(), _envelope);

        Assert.True(forwarded);
    }
}
