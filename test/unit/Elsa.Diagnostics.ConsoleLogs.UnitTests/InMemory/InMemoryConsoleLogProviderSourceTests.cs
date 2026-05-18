using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.InMemory;

public class InMemoryConsoleLogProviderSourceTests
{
    [Fact]
    public async Task GetRecentAsync_OrdersOverlappingSourcesDeterministically()
    {
        var provider = new InMemoryConsoleLogProvider(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()), new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions())));
        var timestamp = DateTimeOffset.UtcNow;

        await provider.PublishAsync(Line("b", "b", timestamp, 1));
        await provider.PublishAsync(Line("a", "a", timestamp, 1));

        var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Equal(["a", "b"], result.Items.Select(x => x.Text));
    }

    private static ConsoleLogLine Line(string text, string sourceId, DateTimeOffset timestamp, long sequence) => new()
    {
        Text = text,
        Source = new ConsoleLogSource { Id = sourceId, DisplayName = sourceId, MachineName = "machine" },
        Timestamp = timestamp,
        ReceivedAt = timestamp,
        Sequence = sequence
    };
}
