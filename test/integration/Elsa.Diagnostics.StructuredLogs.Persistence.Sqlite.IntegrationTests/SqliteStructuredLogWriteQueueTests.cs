using Elsa.Common;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    [Fact]
    public async Task BackgroundTask_FlushesLoggerWrites_ForShellLifecycle()
    {
        await using var host = new SqliteStructuredLogTestHost(migrate: false);
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        foreach (var startupTask in services.GetServices<IStartupTask>())
            await startupTask.ExecuteAsync(CancellationToken.None);

        var backgroundTask = services.GetServices<IBackgroundTask>().OfType<StructuredLogWriteBufferBackgroundTask>().Single();
        await backgroundTask.StartAsync(CancellationToken.None);
        await backgroundTask.StopAsync(CancellationToken.None);
        await backgroundTask.StartAsync(CancellationToken.None);

        try
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Elsa.Tests.ShellLifecycle");
            logger.LogInformation("SQLite structured log shell background task test");

            await WaitUntilAsync(async () => await host.CountRowsAsync("StructuredLogEvents") >= 1);
        }
        finally
        {
            await backgroundTask.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitUntilAsync(Func<ValueTask<bool>> condition)
    {
        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeoutTokenSource.IsCancellationRequested)
        {
            if (await condition())
                return;

            await Task.Delay(50);
        }

        Assert.Fail("The expected structured log row was not persisted before the timeout elapsed.");
    }
}
