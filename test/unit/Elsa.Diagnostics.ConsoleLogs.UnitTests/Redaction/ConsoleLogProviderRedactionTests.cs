using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Redaction;

public class ConsoleLogProviderRedactionTests
{
    [Fact]
    public async Task CaptureTee_PublishesRedactedLinesToProvider()
    {
        var originalOut = Console.Out;
        var provider = new CapturingProvider();
        var registry = new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());
        var capture = new ConsoleCaptureTee(provider, registry, new ConsoleLogRedactor(options), new ConsoleLineFormatter(options), options);

        try
        {
            Console.SetOut(TextWriter.Null);
            await capture.StartAsync();
            Console.WriteLine(string.Concat("pass", "word", "=", "sample-value"));
            Assert.Equal("[Redacted]", Assert.Single(provider.Lines).Text);
        }
        finally
        {
            await capture.StopAsync();
            Console.SetOut(originalOut);
        }
    }

    private sealed class CapturingProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> Lines { get; } = [];

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
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
