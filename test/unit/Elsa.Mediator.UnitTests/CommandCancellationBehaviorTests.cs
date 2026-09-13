using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.UnitTests;

public class CommandCancellationBehaviorTests
{
    [Test]
    public async Task SendAsync_WithSuccessfulCommand_ReturnsResult()
    {
        // Arrange
        using var fixture = CreateCommandSender<EchoCommandHandler>();

        // Act
        var result = await fixture.CommandSender.SendAsync(new EchoCommand("Hello"));

        // Assert
        await Assert.That(result).IsEqualTo("Hello");
    }

    [Test]
    public async Task SendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var fixture = CreateCommandSender<SlowCommandHandler>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.That(() => fixture.CommandSender.SendAsync(new SlowCommand(), cts.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task SendAsync_WithTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        using var fixture = CreateCommandSender<SlowCommandHandler>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.That(() => fixture.CommandSender.SendAsync(new SlowCommand(), cts.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task SendAsync_WithSelfCancellingHandler_ThrowsTaskCanceledException()
    {
        // Arrange
        using var fixture = CreateCommandSender<SelfCancellingCommandHandler>();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => fixture.CommandSender.SendAsync(new SelfCancellingCommand(cts)));
    }

    [Test]
    public async Task SendAsync_WithFailingHandler_ThrowsOriginalException()
    {
        // Arrange
        using var fixture = CreateCommandSender<FailingCommandHandler>();

        // Act & Assert
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => fixture.CommandSender.SendAsync(new FailingCommand("Test error")));

        await Assert.That(ex!.Message).IsEqualTo("Test error");
    }

    #region Helpers

    private static CommandSenderFixture CreateCommandSender<THandler>() where THandler : class, ICommandHandler
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddMediator();
        services.AddCommandHandler<THandler>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return new CommandSenderFixture(provider, scope);
    }

    private sealed class CommandSenderFixture(ServiceProvider provider, IServiceScope scope) : IDisposable
    {
        public ICommandSender CommandSender => scope.ServiceProvider.GetRequiredService<ICommandSender>();

        public void Dispose()
        {
            scope.Dispose();
            provider.Dispose();
        }
    }

    #endregion

    #region Test Commands

    public record EchoCommand(string Message) : ICommand<string>;
    public record SlowCommand : ICommand;
    public record SelfCancellingCommand(CancellationTokenSource Cts) : ICommand;
    public record FailingCommand(string ErrorMessage) : ICommand;

    #endregion

    #region Test Handlers

    public class EchoCommandHandler : ICommandHandler<EchoCommand, string>
    {
        public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken)
            => Task.FromResult(command.Message);
    }

    public class SlowCommandHandler : ICommandHandler<SlowCommand, Unit>
    {
        public async Task<Unit> HandleAsync(SlowCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            return Unit.Instance;
        }
    }

    public class SelfCancellingCommandHandler : ICommandHandler<SelfCancellingCommand, Unit>
    {
        public async Task<Unit> HandleAsync(SelfCancellingCommand command, CancellationToken cancellationToken)
        {
            await command.Cts.CancelAsync();
            await Task.Delay(1000, command.Cts.Token);
            return Unit.Instance;
        }
    }

    public class FailingCommandHandler : ICommandHandler<FailingCommand, Unit>
    {
        public Task<Unit> HandleAsync(FailingCommand command, CancellationToken cancellationToken)
            => throw new InvalidOperationException(command.ErrorMessage);
    }

    #endregion
}
