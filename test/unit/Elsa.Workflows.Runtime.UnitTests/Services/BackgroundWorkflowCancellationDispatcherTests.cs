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
        var tenant = new Tenant
        {
            Id = tenantId
        };
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
                headers.ContainsKey(TenantHeaders.TenantIdKey) &&
                headers[TenantHeaders.TenantIdKey].ToString() == tenantId),
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
    public async Task DispatchAsync_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchCancelWorkflowRequest();
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.DispatchAsync(request, callerCts.Token);

        // Assert - background commands run independently of caller's lifecycle
        await _commandSender.Received(1).SendAsync(
            Arg.Any<CancelWorkflowsCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    private BackgroundWorkflowCancellationDispatcher CreateDispatcher()
    {
        return new(_commandSender, _tenantAccessor);
    }
}