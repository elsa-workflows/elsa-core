using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using System.Text.Json;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RecentConsoleLinesResult_DeserializesCoreResponseShape()
    {
        const string json = """
        {
          "items": [
            {
              "id": "line-1",
              "timestamp": "2026-05-18T09:00:00.0000000Z",
              "receivedAt": "2026-05-18T09:00:00.1000000Z",
              "sequence": 42,
              "stream": 1,
              "text": "raw stderr line",
              "source": {
                "id": "source-a",
                "displayName": "Worker A",
                "serviceName": "worker",
                "processId": 123,
                "machineName": "machine-a",
                "podName": "pod-a",
                "containerName": "container-a",
                "namespace": "default",
                "nodeName": "node-a",
                "lastSeen": "2026-05-18T09:00:00.0000000Z",
                "health": 1
              },
              "metadata": {
                "elsa.workflowInstanceId": "workflow-a"
              },
              "truncated": true,
              "dropped": {
                "sourceId": "source-a",
                "stream": 1,
                "reason": "SubscriberChannelFull",
                "count": 3
              }
            }
          ],
          "dropped": [
            {
              "sourceId": "source-a",
              "stream": 0,
              "reason": "RecentBufferFull",
              "count": 5
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<RecentConsoleLinesResult>(json, Options)!;
        var line = Assert.Single(result.Items);

        Assert.Equal(5, result.DroppedLineCount);
        Assert.Equal("line-1", line.Id);
        Assert.Equal(ConsoleLogStream.Stderr, line.Stream);
        Assert.Equal("raw stderr line", line.Text);
        Assert.Equal("source-a", line.Source.Id);
        Assert.Equal("Worker A", line.Source.DisplayName);
        Assert.Equal(ConsoleLogSourceHealth.Connected, line.Source.Health);
        Assert.Equal("workflow-a", line.Metadata["elsa.workflowInstanceId"]);
        Assert.True(line.IsTruncated);
        Assert.Equal(3, line.Dropped?.Count);
    }
}
