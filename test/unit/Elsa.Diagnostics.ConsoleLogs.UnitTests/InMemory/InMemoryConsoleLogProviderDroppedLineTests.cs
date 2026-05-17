using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.InMemory;

public class InMemoryConsoleLogProviderDroppedLineTests
{
    [Fact]
    public async Task GetRecentAsync_IncludesDroppedSummaryWhenRecentBufferOverflows()
    {
        var provider = new InMemoryConsoleLogProvider(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { RecentLogCapacity = 1 }), new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions())));

        await provider.PublishAsync(Line("one"));
        await provider.PublishAsync(Line("two"));

        var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        Assert.Single(result.Dropped!);
        Assert.Equal("RecentBufferFull", result.Dropped!.Single().Reason);
    }

    [Fact]
    public async Task GetRecentAsync_ConsumesDroppedSummaries()
    {
        var provider = new InMemoryConsoleLogProvider(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { RecentLogCapacity = 1 }), new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions())));

        await provider.PublishAsync(Line("one"));
        await provider.PublishAsync(Line("two"));

        Assert.NotEmpty((await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 })).Dropped!);
        Assert.Empty((await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 })).Dropped!);
    }

    [Fact]
    public async Task GetRecentAsync_IncludesReportedCaptureDrops()
    {
        var provider = new InMemoryConsoleLogProvider(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()), new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions())));

        provider.ReportDropped(new ConsoleLogDroppedSummary("source", ConsoleLogStream.Stdout, "CaptureChannelFull", 2));

        var result = await provider.GetRecentAsync(new ConsoleLogFilter { Limit = 10 });

        var dropped = Assert.Single(result.Dropped!);
        Assert.Equal("CaptureChannelFull", dropped.Reason);
        Assert.Equal(2, dropped.Count);
    }

    private static ConsoleLogLine Line(string text) => new()
    {
        Text = text,
        Source = new ConsoleLogSource { Id = "source", DisplayName = "source", MachineName = "machine" }
    };
}
