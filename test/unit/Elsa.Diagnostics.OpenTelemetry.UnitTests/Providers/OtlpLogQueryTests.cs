using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OtlpLogQueryTests
{
    [Fact]
    public async Task QueryLogsAsync_WhenFilteringByTraceAndSeverity_ReturnsMatchingLogs()
    {
        var context = new OpenTelemetryStoreTestContext();
        var resource = context.Resource("resource-api", "api");
        await context.WriteAsync(new OpenTelemetryBatch(
            [resource],
            [],
            [],
            [],
            [],
            [
                context.Log("log-1", resource.Id, "trace-1", "Information"),
                context.Log("log-2", resource.Id, "trace-1", "Error"),
                context.Log("log-3", resource.Id, "trace-2", "Error")
            ]));

        var result = await context.Store.QueryLogsAsync(new OpenTelemetryLogFilter { TraceId = "trace-1", Severity = "Error" });

        Assert.Equal("log-2", Assert.Single(result.Items).Id);
    }
}
