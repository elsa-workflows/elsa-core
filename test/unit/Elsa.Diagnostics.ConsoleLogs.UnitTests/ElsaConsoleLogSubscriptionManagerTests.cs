using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ElsaConsoleLogSubscriptionManagerTests
{
    [Fact]
    public async Task SubscribeAsync_DoesNotSendSourceChangedForRepeatedSource()
    {
        var source = new ConsoleLogSource
        {
            Id = "source-1",
            DisplayName = "Source 1",
            ServiceName = "service",
            MachineName = "machine",
            Health = ConsoleLogSourceHealth.Connected
        };
        var provider = new StreamingConsoleLogProvider(
            ConsoleLogStreamingItem.FromLine(CreateLine(source, "first")),
            ConsoleLogStreamingItem.FromLine(CreateLine(source, "second")));
        var client = new RecordingConsoleLogsClient();
        var hubContext = new TestHubContext(client);
        using var manager = new ElsaConsoleLogSubscriptionManager(provider, hubContext, NullLogger<ElsaConsoleLogSubscriptionManager>.Instance);

        await manager.SubscribeAsync("connection-1", new(), CancellationToken.None);

        await AssertEventuallyAsync(() =>
        {
            Assert.Equal(2, client.Lines.Count);
            Assert.Single(client.Sources);
        });
    }

    private static ConsoleLogLine CreateLine(ConsoleLogSource source, string text) => new()
    {
        Text = text,
        Source = source
    };

    private static async Task AssertEventuallyAsync(Action assertion)
    {
        Xunit.Sdk.XunitException? lastException = null;

        for (var i = 0; i < 40; i++)
        {
            try
            {
                assertion();
                return;
            }
            catch (Xunit.Sdk.XunitException e)
            {
                lastException = e;
                await Task.Delay(25);
            }
        }

        if (lastException != null)
            throw lastException;
    }

    private sealed class StreamingConsoleLogProvider(params ConsoleLogStreamingItem[] items) : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult());
        }

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }

    private sealed class RecordingConsoleLogsClient : IElsaConsoleLogsClient
    {
        public List<ConsoleLogLine> Lines { get; } = [];
        public List<ConsoleLogSource> Sources { get; } = [];

        public Task ReceiveConsoleLogLineAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            Lines.Add(line);
            return Task.CompletedTask;
        }

        public Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReceiveSourceChangedAsync(ConsoleLogSource source, CancellationToken cancellationToken = default)
        {
            Sources.Add(source);
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
