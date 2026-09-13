using Elsa.Diagnostics.StructuredLogs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogStoreTests
{
    [Test]
    public async Task QueryAsync_ReturnsLogsWrittenByPreviousProviderInstance()
    {
        await using var firstHost = new SqliteStructuredLogTestHost();
        var written = StructuredLogTestEvents.Create("persisted-1", message: "persist me");
        await firstHost.WriteAsync(written);
        var connectionString = firstHost.ConnectionString;

        await using var secondHost = new SqliteStructuredLogTestHost(options => options.ConnectionString = connectionString);

        var result = await secondHost.Store.QueryAsync(new StructuredLogFilter { Take = 10 });

        await Assert.That(result.Items).Contains(x => x.Id == written.Id && x.Message == "persist me");
    }

    [Test]
    public async Task Logger_RedactsSensitiveValuesBeforePersisting()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Elsa.Tests.Redaction");

        logger.LogInformation("Authorization bearer abc123");
        await host.Buffer.FlushAsync();

        var result = await host.Store.QueryAsync(new StructuredLogFilter { Take = 10, Text = "[Redacted]" });
        var item = (await Assert.That(result.Items).HasSingleItem())!;
        await Assert.That(item.Message).IsEqualTo("Authorization [Redacted]");
    }
}
