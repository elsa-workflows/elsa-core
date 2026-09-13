using Elsa.Diagnostics.OpenTelemetry.Models;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Providers;

public class OtlpLogQueryTests
{
    [Test]
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

        var log = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(log.Id).IsEqualTo("log-2");
    }
}
