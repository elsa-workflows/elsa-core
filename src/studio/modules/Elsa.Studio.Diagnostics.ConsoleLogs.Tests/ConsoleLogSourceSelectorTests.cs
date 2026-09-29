using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogSourceSelectorTests
{
    [Fact]
    public void Filter_UsesStableSourceId()
    {
        var filter = new ConsoleLogFilter { SourceId = "source-id", Query = "needle" };

        var request = ConsoleLogFilterMapper.ToLiveSubscription(filter);

        Assert.Equal("source-id", request.SourceId);
    }

    [Fact]
    public void SourceDisplayName_UsesDisplayNameForLabels()
    {
        var source = new ConsoleLogSource { Id = "source-id", DisplayName = "Worker" };

        var label = ConsoleLogExportFormatter.SourceDisplayName(source);

        Assert.Equal("Worker", label);
    }

    [Fact]
    public void StaleSource_RemainsRepresentableById()
    {
        var source = new ConsoleLogSource { Id = "stale-source", Health = ConsoleLogSourceHealth.Stale };

        Assert.Equal("stale-source", source.Id);
        Assert.Equal(ConsoleLogSourceHealth.Stale, source.Health);
    }
}
