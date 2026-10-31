using Elsa.Studio.Diagnostics.StructuredLogs.Models;
using System.Text.Json;
using Xunit;

#pragma warning disable IL2026 // Test intentionally exercises the runtime serializer used by the API client.

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public class StructuredLogSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RecentStructuredLogsResult_DeserializesCoreResponseShape()
    {
        const string json = """
        {
          "items": [
            {
              "id": "log-1",
              "sequence": 1,
              "timestamp": "2026-05-10T09:00:00.0000000Z",
              "receivedAt": "2026-05-10T09:00:00.0000000Z",
              "level": 2,
              "category": "Elsa.Workflows",
              "eventId": 42,
              "eventName": "WorkflowStarted",
              "message": "Started workflow order-1",
              "messageTemplate": "Started workflow {WorkflowInstanceId}",
              "traceId": "trace-a",
              "spanId": "span-a",
              "sourceId": "source-a",
              "properties": {
                "WorkflowInstanceId": "order-1"
              },
              "scopes": {
                "TenantId": "tenant-a",
                "CorrelationId": "correlation-a"
              }
            }
          ],
          "droppedEvents": 3
        }
        """;

        var result = JsonSerializer.Deserialize<RecentStructuredLogsResult>(json, Options)!;
        var logEvent = Assert.Single(result.Items);

        Assert.Equal(3, result.DroppedEvents);
        Assert.Equal("Started workflow {WorkflowInstanceId}", logEvent.MessageTemplate);
        Assert.Equal("span-a", logEvent.SpanId);
        Assert.Equal("order-1", logEvent.Properties["WorkflowInstanceId"]);
        Assert.Equal("tenant-a", logEvent.Scopes["TenantId"]);
        Assert.Equal("correlation-a", logEvent.Scopes["CorrelationId"]);
    }

    [Fact]
    public void StructuredLogStorageDiagnostics_DeserializesCoreResponseShape()
    {
        const string json = """
        {
          "droppedWriteCount": 5,
          "hasStorageDiagnosticsProvider": true
        }
        """;

        var result = JsonSerializer.Deserialize<StructuredLogStorageDiagnostics>(json, Options)!;

        Assert.Equal(5, result.DroppedWriteCount);
        Assert.True(result.HasStorageDiagnosticsProvider);
    }
}
