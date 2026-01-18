using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Options;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BackgroundWorkflowCancellationDispatcherTests
{
    private readonly ICommandSender _commandSender = Substitute.For<ICommandSender>();
    private readonly ITenantAccessor _tenantAccessor = Substitute.For<ITenantAccessor>();

    [Fact]
    public async Task DispatchAsync_SendsCommandWithBackgroundStrategy()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();

        // Act
        await dispatcher.DispatchAsync(request);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Is<CancelWorkflowsCommand>(cmd => cmd.Request == request),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_IncludesTenantIdInHeaders_WhenTenantIsPresent()
    {
        // Arrange
        var tenantId = "test-tenant-123";
        var tenant = new Tenant { Id = tenantId };
        _tenantAccessor.Tenant.Returns(tenant);

        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();

        // Act
        await dispatcher.DispatchAsync(request);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<CancelWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Is<IDictionary<object, object>>(headers =>
                headers.TryGetValue(TenantHeaders.TenantIdKey, out var value) &&
                value?.ToString() == tenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_DoesNotIncludeTenantIdInHeaders_WhenTenantIsNull()
    {
        // Arrange
        _tenantAccessor.Tenant.Returns((Tenant?)null);

        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();

        // Act
        await dispatcher.DispatchAsync(request);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<CancelWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Is<IDictionary<object, object>>(headers => 
                !headers.ContainsKey(TenantHeaders.TenantIdKey)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_ReturnsResponse()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();

        // Act
        var response = await dispatcher.DispatchAsync(request);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task DispatchAsync_PassesCancellationToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        await dispatcher.DispatchAsync(request, cancellationToken);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<CancelWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            cancellationToken);
    }

    private BackgroundWorkflowCancellationDispatcher CreateDispatcher()
    {
        return new(_commandSender, _tenantAccessor);
    }
}
