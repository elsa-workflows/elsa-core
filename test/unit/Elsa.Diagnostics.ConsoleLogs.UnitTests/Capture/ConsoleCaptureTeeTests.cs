using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Capture;

public class ConsoleCaptureTeeTests
{
    [Fact]
    public async Task StartAsync_PreservesOriginalConsoleOutputAndPublishesLine()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var consoleOutput = new StringWriter();
        var provider = new CapturingProvider();
        var registry = new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());
        var capture = new ConsoleCaptureTee(provider, registry, new ConsoleLogRedactor(options), new ConsoleLineFormatter(options), options);

        try
        {
            Console.SetOut(consoleOutput);
            await capture.StartAsync();

            Console.WriteLine("hello");
            await WaitForLineAsync(provider);

            Assert.Equal($"hello{Environment.NewLine}", consoleOutput.ToString());
            Assert.Single(provider.Lines);
            Assert.Equal("hello", provider.Lines[0].Text);
            Assert.Equal(ConsoleLogStream.Stdout, provider.Lines[0].Stream);
        }
        finally
        {
            await capture.StopAsync();
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_DoesNotWrapConsoleTwice()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var consoleOutput = new StringWriter();
        var provider = new CapturingProvider();
        var registry = new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));
        var options = Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions());
        var capture = new ConsoleCaptureTee(provider, registry, new ConsoleLogRedactor(options), new ConsoleLineFormatter(options), options);

        try
        {
            Console.SetOut(consoleOutput);
            await capture.StartAsync();
            await capture.StartAsync();

            Console.WriteLine("hello");
            await WaitForLineAsync(provider);

            Assert.Equal($"hello{Environment.NewLine}", consoleOutput.ToString());
            Assert.Single(provider.Lines);
        }
        finally
        {
            await capture.StopAsync();
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static async Task WaitForLineAsync(CapturingProvider provider)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(2);
        while (provider.Lines.Count == 0 && DateTimeOffset.UtcNow < timeout)
            await Task.Delay(10);
    }

    private sealed class CapturingProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> Lines { get; } = [];

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            Lines.Add(line);
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult(Lines));
        }

        public async IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var line in Lines)
                yield return ConsoleLogStreamItem.FromLine(line);
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }
}
