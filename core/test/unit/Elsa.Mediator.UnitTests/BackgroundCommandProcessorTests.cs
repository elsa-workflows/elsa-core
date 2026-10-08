using Elsa.Mediator.Channels;
using Elsa.Mediator.CommandStrategies;
using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Models;
using Elsa.Mediator.Options;
using Elsa.Mediator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Mediator.UnitTests;

public class BackgroundCommandProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_UsesQueuedContextStrategyAndCancellationToken()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandSenderRecorder>();
        services.AddScoped<ICommandSender, RecordingCommandSender>();
        await using var serviceProvider = services.BuildServiceProvider();
        var channel = new CommandsChannel();
        var processor = new BackgroundCommandProcessor(
            Microsoft.Extensions.Options.Options.Create(new MediatorOptions { CommandWorkerCount = 1 }),
            channel,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BackgroundCommandProcessor>.Instance);
        using var processorCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var commandCancellationTokenSource = new CancellationTokenSource();
        var runTask = processor.ExecuteAsync(processorCancellationTokenSource.Token);
        var strategy = new TestCommandStrategy();
        var context = new CommandContext(new TestCommand(), strategy, typeof(Unit), new Dictionary<object, object>(), serviceProvider, commandCancellationTokenSource.Token);

        await commandCancellationTokenSource.CancelAsync();
        await channel.Writer.WriteAsync(context, processorCancellationTokenSource.Token);

        var recorder = serviceProvider.GetRequiredService<CommandSenderRecorder>();
        var recordedCall = await recorder.WaitForCallAsync(processorCancellationTokenSource.Token);
        await processorCancellationTokenSource.CancelAsync();
        await runTask;

        Assert.Same(strategy, recordedCall.Strategy);
        Assert.True(recordedCall.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task BackgroundStrategy_QueuesExecutableContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandsChannel, CommandsChannel>();
        await using var serviceProvider = services.BuildServiceProvider();
        var channel = serviceProvider.GetRequiredService<ICommandsChannel>();
        using var commandCancellationTokenSource = new CancellationTokenSource();
        var commandContext = new CommandContext(new TestCommand(), CommandStrategy.Background, typeof(Unit), new Dictionary<object, object>(), serviceProvider, commandCancellationTokenSource.Token);
        var strategyContext = new CommandStrategyContext(commandContext, new TestCommandHandler(), serviceProvider);

        await commandCancellationTokenSource.CancelAsync();
        await CommandStrategy.Background.ExecuteAsync<Unit>(strategyContext);

        var queuedContext = await channel.Reader.ReadAsync();

        Assert.Same(CommandStrategy.Default, queuedContext.CommandStrategy);
        Assert.Same(commandContext.Command, queuedContext.Command);
        Assert.Same(commandContext.Headers, queuedContext.Headers);
        Assert.False(queuedContext.CancellationToken.IsCancellationRequested);
    }

    private sealed class RecordingCommandSender(CommandSenderRecorder recorder) : ICommandSender
    {
        public Task<T> SendAsync<T>(ICommand<T> command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T> SendAsync<T>(ICommand<T> command, IDictionary<object, object> headers, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy? strategy, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T> SendAsync<T>(ICommand<T> command, ICommandStrategy? strategy, IDictionary<object, object> headers, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SendAsync(ICommand command, ICommandStrategy? strategy, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task SendAsync(ICommand command, ICommandStrategy? strategy, IDictionary<object, object> headers, CancellationToken cancellationToken = default)
        {
            recorder.Record(new(strategy, cancellationToken));
            return Task.CompletedTask;
        }
    }

    private sealed class CommandSenderRecorder
    {
        private readonly TaskCompletionSource<RecordedCommandCall> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(RecordedCommandCall call) => _completed.TrySetResult(call);

        public Task<RecordedCommandCall> WaitForCallAsync(CancellationToken cancellationToken) => _completed.Task.WaitAsync(cancellationToken);
    }

    private sealed record RecordedCommandCall(ICommandStrategy? Strategy, CancellationToken CancellationToken);

    private sealed record TestCommand : ICommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task<Unit> HandleAsync(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(Unit.Instance);
    }

    private sealed class TestCommandStrategy : DefaultStrategy
    {
    }
}
