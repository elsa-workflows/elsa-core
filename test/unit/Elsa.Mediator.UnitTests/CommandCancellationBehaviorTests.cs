using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.UnitTests;

public class CommandCancellationBehaviorTests
{
    [Fact]
    public async Task SendAsync_WithSuccessfulCommand_ReturnsResult()
    {
        // Arrange
        var commandSender = CreateCommandSender<EchoCommandHandler>();

        // Act
        var result = await commandSender.SendAsync(new EchoCommand("Hello"));

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task SendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var commandSender = CreateCommandSender<SlowCommandHandler>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => commandSender.SendAsync(new SlowCommand(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_WithTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        var commandSender = CreateCommandSender<SlowCommandHandler>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => commandSender.SendAsync(new SlowCommand(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_WithSelfCancellingHandler_ThrowsTaskCanceledException()
    {
        // Arrange
        var commandSender = CreateCommandSender<SelfCancellingCommandHandler>();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => commandSender.SendAsync(new SelfCancellingCommand(cts)));
    }

    [Fact]
    public async Task SendAsync_WithFailingHandler_ThrowsOriginalException()
    {
        // Arrange
        var commandSender = CreateCommandSender<FailingCommandHandler>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commandSender.SendAsync(new FailingCommand("Test error")));

        Assert.Equal("Test error", ex.Message);
    }

    #region Helpers

    private static ICommandSender CreateCommandSender<THandler>() where THandler : class, ICommandHandler
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddMediator();
        services.AddCommandHandler<THandler>();

        var provider = services.BuildServiceProvider();
        return provider.CreateScope().ServiceProvider.GetRequiredService<ICommandSender>();
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
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
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
