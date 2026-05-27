using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsHubSourceStatusTests
{
    [Fact]
    public void ClientContract_ExposesSourceChangeMethod()
    {
        Assert.NotNull(typeof(IElsaConsoleLogsClient).GetMethod(nameof(IElsaConsoleLogsClient.ReceiveSourceChangedAsync)));
    }

    [Fact]
    public async Task PushedSubscription_ForwardsSourceChangeFromConsoleLines()
    {
        var source = new ConsoleLogSource { Id = "source-1", DisplayName = "Source 1" };
        var provider = new SingleLineProvider(new ConsoleLogLine { Text = "message", Source = source });
        var client = new TestConsoleLogsClient();
        var hubContext = new TestHubContext(client);
        var manager = new ElsaConsoleLogSubscriptionManager(provider, hubContext, NullLogger<ElsaConsoleLogSubscriptionManager>.Instance);

        await manager.SubscribeAsync("connection-1", new ElsaConsoleLogFilter(), CancellationToken.None);

        Assert.Same(source, await client.SourceChanged.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private sealed class SingleLineProvider(ConsoleLogLine line) : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult());
        }

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ConsoleLogStreamingItem.FromLine(line);
            await Task.Yield();
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([line.Source]);
        }
    }

    private sealed class TestConsoleLogsClient : IElsaConsoleLogsClient
    {
        public TaskCompletionSource<ConsoleLogSource> SourceChanged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ReceiveConsoleLogLineAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReceiveSourceChangedAsync(ConsoleLogSource source, CancellationToken cancellationToken = default)
        {
            SourceChanged.TrySetResult(source);
            return Task.CompletedTask;
        }
    }

    private sealed class TestHubContext(IElsaConsoleLogsClient client) : IHubContext<ElsaConsoleLogsHub, IElsaConsoleLogsClient>
    {
        public IHubClients<IElsaConsoleLogsClient> Clients { get; } = new TestHubClients(client);

        public IGroupManager Groups { get; } = new TestGroupManager();
    }

    private sealed class TestHubClients(IElsaConsoleLogsClient client) : IHubClients<IElsaConsoleLogsClient>
    {
        public IElsaConsoleLogsClient All => client;
        public IElsaConsoleLogsClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => client;
        public IElsaConsoleLogsClient Client(string connectionId) => client;
        public IElsaConsoleLogsClient Clients(IReadOnlyList<string> connectionIds) => client;
        public IElsaConsoleLogsClient Group(string groupName) => client;
        public IElsaConsoleLogsClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => client;
        public IElsaConsoleLogsClient Groups(IReadOnlyList<string> groupNames) => client;
        public IElsaConsoleLogsClient User(string userId) => client;
        public IElsaConsoleLogsClient Users(IReadOnlyList<string> userIds) => client;
    }

    private sealed class TestGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
