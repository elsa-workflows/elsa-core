using System.Globalization;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogTimestampTests
{
    [Test]
    public async Task WriteAsync_StoresTimestampsAsUtcIso8601()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var timestamp = new DateTimeOffset(2026, 5, 13, 12, 30, 45, TimeSpan.FromHours(2));

        await host.WriteAsync(StructuredLogTestEvents.Create("timestamp", timestamp));

        var rawTimestamp = (await Assert.That(await host.ReadRawTimestampsAsync()).HasSingleItem())!;
        await Assert.That(rawTimestamp).EndsWith("+00:00", StringComparison.Ordinal);
        await Assert.That(DateTimeOffset.Parse(rawTimestamp, CultureInfo.InvariantCulture)).IsEqualTo(timestamp.ToUniversalTime());
    }
}
