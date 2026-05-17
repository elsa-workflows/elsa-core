using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Sources;

public class ConsoleLogSourceRegistryTests
{
    [Fact]
    public void MarkSeen_AddsUnknownSourceAndRaisesChange()
    {
        var registry = new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));
        ConsoleLogSource? changed = null;
        registry.SourceChanged += source => changed = source;

        registry.MarkSeen("remote", DateTimeOffset.UtcNow);

        Assert.NotNull(changed);
        Assert.Contains(registry.List(), x => x.Id == "remote" && x.Health == ConsoleLogSourceHealth.Connected);
    }

    [Fact]
    public void List_MarksOldSourcesStale()
    {
        var registry = new ConsoleLogSourceRegistry(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { SourceHeartbeatTimeout = TimeSpan.FromSeconds(1) }));

        registry.MarkSeen("remote", DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.Contains(registry.List(), x => x.Id == "remote" && x.Health == ConsoleLogSourceHealth.Stale);
    }
}
