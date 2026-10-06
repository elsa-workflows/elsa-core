using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Tenants.Mediator;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class BackgroundStimulusDispatcherTests
{
    private readonly ICommandSender _commandSender = Substitute.For<ICommandSender>();
    private readonly ITenantAccessor _tenantAccessor = Substitute.For<ITenantAccessor>();

    [Fact]
    public async Task SendAsync_DoesNotPropagateCallerToken()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DispatchStimulusRequest();
        using var callerCts = new CancellationTokenSource();

        // Act
        await dispatcher.SendAsync(request, callerCts.Token);

        // Assert - background commands run independently of caller's lifecycle
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchStimulusCommand>(),
            CommandStrategy.Background,
            Arg.Any<IDictionary<object, object>>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_IncludesTenantIdInHeaders_WhenTenantIsPresent()
    {
        // Arrange
        var tenantId = "test-tenant-123";
        _tenantAccessor.TenantId.Returns(tenantId);

        var dispatcher = CreateDispatcher();
        var request = new DispatchStimulusRequest();

        // Act
        await dispatcher.SendAsync(request);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchStimulusCommand>(),
            CommandStrategy.Background,
            Arg.Is<IDictionary<object, object>>(headers =>
                headers.ContainsKey(TenantHeaders.TenantIdKey) &&
                headers[TenantHeaders.TenantIdKey].ToString() == tenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_IncludesEmptyTenantIdInHeaders_WhenDefaultTenant()
    {
        // Arrange - default tenant normalizes to empty string, not null
        _tenantAccessor.TenantId.Returns(string.Empty);

        var dispatcher = CreateDispatcher();
        var request = new DispatchStimulusRequest();

        // Act
        await dispatcher.SendAsync(request);

        // Assert
        await _commandSender.Received(1).SendAsync(
            Arg.Any<DispatchStimulusCommand>(),
            CommandStrategy.Background,
            Arg.Is<IDictionary<object, object>>(headers =>
                headers.ContainsKey(TenantHeaders.TenantIdKey) &&
                headers[TenantHeaders.TenantIdKey].ToString() == string.Empty),
            Arg.Any<CancellationToken>());
    }

    private BackgroundStimulusDispatcher CreateDispatcher()
    {
        return new(_commandSender, _tenantAccessor);
    }
}
