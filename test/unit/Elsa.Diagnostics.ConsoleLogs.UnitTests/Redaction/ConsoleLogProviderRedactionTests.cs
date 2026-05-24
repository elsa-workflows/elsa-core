using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Redaction;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleLogProviderRedactionTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await ConsoleLogsHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    public async Task DisposeAsync()
    {
        await ConsoleLogsHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [Fact]
    public async Task CaptureTee_PublishesRedactedLinesToProvider()
    {
        var originalOut = Console.Out;
        var provider = new CapturingProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());

        try
        {
            ConsoleStreamHook.Uninstall();
            Console.SetOut(TextWriter.Null);
            await using var capture = new ConsoleCaptureTee(
                provider,
                new ConsoleLogSourceRegistry(options),
                new ConsoleLogRedactor(options),
                new ConsoleLineFormatter(options),
                new ConsoleLogScopeAccessor(),
                options);

            Console.WriteLine(string.Concat("pass", "word", "=", "sample-value"));
            await WaitForLineAsync(provider);

            Assert.Equal("[Redacted]", Assert.Single(provider.Lines).Text);
        }
        finally
        {
            ConsoleStreamHook.Uninstall();
            Console.SetOut(originalOut);
        }
    }

    private static async Task WaitForLineAsync(CapturingProvider provider)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (provider.Lines.Count == 0 && DateTimeOffset.UtcNow < deadline)
            await Task.Delay(10);
    }

    private sealed class CapturingProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> Lines { get; } = [];

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            lock (Lines)
                Lines.Add(line);

            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(new RecentConsoleLogsResult(Lines));

        public async IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
    }
}
