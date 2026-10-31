using Elsa.Common;
using Elsa.Common.ShellHandlers;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.UnitTests.ShellHandlers;

public class MediatorBackgroundTaskTests
{
    [Fact]
    public async Task StartAsync_StartsMediatorCommandProcessor()
    {
        // Arrange
        await using var serviceProvider = CreateServiceProvider();
        var backgroundTask = GetMediatorBackgroundTask(serviceProvider);
        var commandSender = serviceProvider.GetRequiredService<ICommandSender>();
        var sink = serviceProvider.GetRequiredService<CommandSink>();

        // Act
        await backgroundTask.StartAsync(CancellationToken.None);

        try
        {
            await commandSender.SendAsync(new TestCommand("hello"), CommandStrategy.Background, CancellationToken.None);

            // Assert
            var completedTask = await Task.WhenAny(sink.Handled.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(sink.Handled.Task, completedTask);
            Assert.Equal("hello", await sink.Handled.Task);
        }
        finally
        {
            await backgroundTask.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopAsync_KeepsProcessorRunning_UntilAllBackgroundTaskReferencesStop()
    {
        // Arrange
        await using var serviceProvider = CreateServiceProvider();
        await using var scope1 = serviceProvider.CreateAsyncScope();
        await using var scope2 = serviceProvider.CreateAsyncScope();
        var backgroundTask1 = GetMediatorBackgroundTask(scope1.ServiceProvider);
        var backgroundTask2 = GetMediatorBackgroundTask(scope2.ServiceProvider);
        var commandSender = serviceProvider.GetRequiredService<ICommandSender>();
        var sink = serviceProvider.GetRequiredService<CommandSink>();

        // Act
        await backgroundTask1.StartAsync(CancellationToken.None);
        await backgroundTask2.StartAsync(CancellationToken.None);
        await backgroundTask1.StopAsync(CancellationToken.None);

        try
        {
            await commandSender.SendAsync(new TestCommand("hello"), CommandStrategy.Background, CancellationToken.None);

            // Assert
            var completedTask = await Task.WhenAny(sink.Handled.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(sink.Handled.Task, completedTask);
            Assert.Equal("hello", await sink.Handled.Task);
        }
        finally
        {
            await backgroundTask2.StopAsync(CancellationToken.None);
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        new ShellFeatures.MediatorFeature().ConfigureServices(services);
        services.AddSingleton<CommandSink>();
        services.AddCommandHandler<TestCommandHandler>();
        return services.BuildServiceProvider();
    }

    private static MediatorBackgroundTask GetMediatorBackgroundTask(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<IBackgroundTask>().OfType<MediatorBackgroundTask>().Single();
    }

    private sealed record TestCommand(string Message) : ICommand;

    private sealed class CommandSink
    {
        public TaskCompletionSource<string> Handled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class TestCommandHandler(CommandSink sink) : ICommandHandler<TestCommand>
    {
        public Task<Unit> HandleAsync(TestCommand command, CancellationToken cancellationToken)
        {
            sink.Handled.TrySetResult(command.Message);
            return Task.FromResult(Unit.Instance);
        }
    }
}
