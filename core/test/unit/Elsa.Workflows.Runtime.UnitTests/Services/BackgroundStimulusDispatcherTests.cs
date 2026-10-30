using Elsa.Common.Multitenancy;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
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

    private BackgroundStimulusDispatcher CreateDispatcher()
    {
        return new(_commandSender, _tenantAccessor);
    }
}
