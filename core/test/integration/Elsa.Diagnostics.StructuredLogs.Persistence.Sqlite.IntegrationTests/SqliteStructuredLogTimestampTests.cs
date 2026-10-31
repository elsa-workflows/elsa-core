using System.Globalization;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogTimestampTests
{
    [Fact]
    public async Task WriteAsync_StoresTimestampsAsUtcIso8601()
    {
        await using var host = new SqliteStructuredLogTestHost();
        var timestamp = new DateTimeOffset(2026, 5, 13, 12, 30, 45, TimeSpan.FromHours(2));

        await host.WriteAsync(StructuredLogTestEvents.Create("timestamp", timestamp));

        var rawTimestamp = Assert.Single(await host.ReadRawTimestampsAsync());
        Assert.EndsWith("+00:00", rawTimestamp, StringComparison.Ordinal);
        Assert.Equal(timestamp.ToUniversalTime(), DateTimeOffset.Parse(rawTimestamp, CultureInfo.InvariantCulture));
    }
}
