using Elsa.Common.Multitenancy;
using Elsa.ServiceBus.MassTransit.Extensions;
using MassTransit;
using Moq;

namespace Elsa.ServiceBus.MassTransit.UnitTests.Extensions;

public class MultitenancyPipeConfigurationExtensionsTests
{
    private readonly Mock<IBusFactoryConfigurator> _bus = new();
    private readonly Mock<IBusRegistrationContext> _context = new();

    [Fact(DisplayName = "A bus in a host without multitenancy starts without tenant middleware")]
    public void ConfigureTenantMiddleware_WithoutTenancy_AddsNoTenantMiddleware()
    {
        _context.Setup(x => x.GetService(typeof(ITenantAccessor))).Returns(null!);

        _bus.Object.ConfigureTenantMiddleware(_context.Object);

        _bus.Verify(x => x.ConfigureSend(It.IsAny<Action<ISendPipeConfigurator>>()), Times.Never);
        _bus.Verify(x => x.ConfigurePublish(It.IsAny<Action<IPublishPipeConfigurator>>()), Times.Never);
    }

    [Fact(DisplayName = "A bus in a host with multitenancy carries the tenant on send and publish")]
    public void ConfigureTenantMiddleware_WithTenancy_AddsSendAndPublishMiddleware()
    {
        _context.Setup(x => x.GetService(typeof(ITenantAccessor))).Returns(new DefaultTenantAccessor());

        _bus.Object.ConfigureTenantMiddleware(_context.Object);

        _bus.Verify(x => x.ConfigureSend(It.IsAny<Action<ISendPipeConfigurator>>()), Times.Once);
        _bus.Verify(x => x.ConfigurePublish(It.IsAny<Action<IPublishPipeConfigurator>>()), Times.Once);
    }
}
