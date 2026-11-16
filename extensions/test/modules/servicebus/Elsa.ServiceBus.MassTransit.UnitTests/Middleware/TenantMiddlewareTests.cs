using Elsa.Common.Multitenancy;
using Elsa.ServiceBus.MassTransit.Middleware;
using MassTransit;
using Moq;

namespace Elsa.ServiceBus.MassTransit.UnitTests.Middleware;

/// <summary>
/// The tenant middleware must stamp the normalized <see cref="ITenantAccessor.TenantId" /> and skip the header entirely
/// for the default tenant (empty ID), which is how unmarked messages remain consumable by default-tenant consumers.
/// </summary>
public class TenantPublishMiddlewareTests
{
    [Fact]
    public async Task NamedTenant_SetsTenantIdHeader()
    {
        var headers = await InvokeAsync(CreateAccessor("tenant-a"));

        headers.Verify(h => h.Set(HeaderNames.TenantId, "tenant-a"), Times.Once);
    }

    [Fact]
    public async Task DefaultTenant_DoesNotSetTenantIdHeader()
    {
        var headers = await InvokeAsync(CreateAccessor(Tenant.DefaultTenantId));

        headers.Verify(h => h.Set(HeaderNames.TenantId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UnresolvedTenant_DoesNotSetTenantIdHeader()
    {
        // No tenant was resolved in context; the accessor normalizes the missing ID to "".
        var headers = await InvokeAsync(CreateAccessor(null, tenantResolved: false));

        headers.Verify(h => h.Set(HeaderNames.TenantId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ContinuesThePipeline()
    {
        var headers = new Mock<SendHeaders>();
        var context = new Mock<PublishContext>();
        context.SetupGet(x => x.Headers).Returns(headers.Object);
        var next = new Mock<IPipe<PublishContext>>();
        next.Setup(p => p.Send(context.Object)).Returns(Task.CompletedTask);

        await new TenantPublishMiddleware(CreateAccessor("tenant-a")).Send(context.Object, next.Object);

        next.Verify(p => p.Send(context.Object), Times.Once);
    }

    private static ITenantAccessor CreateAccessor(string? tenantId, bool tenantResolved = true)
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(x => x.Tenant).Returns(!tenantResolved ? null : string.IsNullOrEmpty(tenantId) ? Tenant.Default : new Tenant { Id = tenantId! });
        tenantAccessor.Setup(x => x.TenantId).Returns(tenantId ?? Tenant.DefaultTenantId);
        return tenantAccessor.Object;
    }

    private static async Task<Mock<SendHeaders>> InvokeAsync(ITenantAccessor tenantAccessor)
    {
        var headers = new Mock<SendHeaders>();
        var context = new Mock<PublishContext>();
        context.SetupGet(x => x.Headers).Returns(headers.Object);
        var next = new Mock<IPipe<PublishContext>>();

        await new TenantPublishMiddleware(tenantAccessor).Send(context.Object, next.Object);

        return headers;
    }
}

public class TenantSendMiddlewareTests
{
    [Fact]
    public async Task NamedTenant_SetsTenantIdHeader()
    {
        var headers = await InvokeAsync("tenant-a");

        headers.Verify(h => h.Set(HeaderNames.TenantId, "tenant-a"), Times.Once);
    }

    [Fact]
    public async Task DefaultTenant_DoesNotSetTenantIdHeader()
    {
        var headers = await InvokeAsync(Tenant.DefaultTenantId);

        headers.Verify(h => h.Set(HeaderNames.TenantId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UnresolvedTenant_DoesNotSetTenantIdHeader()
    {
        var headers = await InvokeAsync(null);

        headers.Verify(h => h.Set(HeaderNames.TenantId, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ContinuesThePipeline()
    {
        var headers = new Mock<SendHeaders>();
        var context = new Mock<SendContext>();
        context.SetupGet(x => x.Headers).Returns(headers.Object);
        var next = new Mock<IPipe<SendContext>>();
        next.Setup(p => p.Send(context.Object)).Returns(Task.CompletedTask);

        await new TenantSendMiddleware(CreateAccessor("tenant-a")).Send(context.Object, next.Object);

        next.Verify(p => p.Send(context.Object), Times.Once);
    }

    private static ITenantAccessor CreateAccessor(string? tenantId, bool tenantResolved = true)
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(x => x.Tenant).Returns(!tenantResolved ? null : string.IsNullOrEmpty(tenantId) ? Tenant.Default : new Tenant { Id = tenantId! });
        tenantAccessor.Setup(x => x.TenantId).Returns(tenantId ?? Tenant.DefaultTenantId);
        return tenantAccessor.Object;
    }

    private static async Task<Mock<SendHeaders>> InvokeAsync(string? tenantId)
    {
        var headers = new Mock<SendHeaders>();
        var context = new Mock<SendContext>();
        context.SetupGet(x => x.Headers).Returns(headers.Object);
        var next = new Mock<IPipe<SendContext>>();

        await new TenantSendMiddleware(CreateAccessor(tenantId)).Send(context.Object, next.Object);

        return headers;
    }
}
