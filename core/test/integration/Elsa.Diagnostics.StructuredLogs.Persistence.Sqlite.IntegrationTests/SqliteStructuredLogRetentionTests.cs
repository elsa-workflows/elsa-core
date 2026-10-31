namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests;

public class SqliteStructuredLogRetentionTests
{
    [Fact]
    public async Task CleanupAsync_DoesNotDeleteRows_WhenRetentionIsNotConfigured()
    {
        await using var host = new SqliteStructuredLogTestHost();
        await host.WriteAsync(
            StructuredLogTestEvents.Create("old", DateTimeOffset.UtcNow.AddDays(-30)),
            StructuredLogTestEvents.Create("new", DateTimeOffset.UtcNow));

        await host.Retention.CleanupAsync();

        Assert.Equal(2, await host.CountRowsAsync("StructuredLogEvents"));
    }

    [Fact]
    public async Task CleanupAsync_AppliesMaxAgeAndMaxRows_WhenConfigured()
    {
        await using var host = new SqliteStructuredLogTestHost(options =>
        {
            options.Relational.Retention.MaxAge = TimeSpan.FromDays(7);
            options.Relational.Retention.MaxRows = 1;
        });
        await host.WriteAsync(
            StructuredLogTestEvents.Create("old", DateTimeOffset.UtcNow.AddDays(-30)),
            StructuredLogTestEvents.Create("middle", DateTimeOffset.UtcNow.AddMinutes(-10)),
            StructuredLogTestEvents.Create("new", DateTimeOffset.UtcNow));

        await host.Retention.CleanupAsync();

        var result = await host.Store.QueryAsync(new() { Take = 10 });
        var item = Assert.Single(result.Items);
        Assert.Equal("new", item.Id);
    }
}
