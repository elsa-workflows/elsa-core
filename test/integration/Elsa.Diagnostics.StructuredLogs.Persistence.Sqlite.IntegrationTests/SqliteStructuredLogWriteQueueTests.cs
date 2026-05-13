using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogWriteQueueTests
{
    [Fact]
    public async Task FlushAsync_PersistsQueuedWrites()
    {
        await using var host = new SqliteStructuredLogTestHost();

        await host.Buffer.WriteAsync(StructuredLogTestEvents.Create("queued"));
        await host.Buffer.FlushAsync();

        Assert.Equal(1, await host.CountRowsAsync("StructuredLogEvents"));
    }

    [Fact]
    public async Task WriteAsync_DropsWrites_WhenQueueIsFull()
    {
        await using var host = new SqliteStructuredLogTestHost(options =>
        {
            options.Relational.WriteQueue.Capacity = 1;
            options.Relational.WriteQueue.BatchSize = 10;
        });

        await host.Buffer.WriteAsync(StructuredLogTestEvents.Create("kept"));
        await host.Buffer.WriteAsync(StructuredLogTestEvents.Create("dropped"));
        await host.Buffer.FlushAsync();

        Assert.Equal(1, host.Buffer.DroppedWriteCount);
        Assert.Equal(1, await host.CountRowsAsync("StructuredLogEvents"));
    }

    [Fact]
    public async Task StopAsync_FlushesQueuedWrites()
    {
        await using var host = new SqliteStructuredLogTestHost();
        await host.StartHostedServicesAsync();

        await host.Buffer.WriteAsync(StructuredLogTestEvents.Create("shutdown"));
        await host.StopHostedServicesAsync();

        Assert.Equal(1, await host.CountRowsAsync("StructuredLogEvents"));
    }
}
