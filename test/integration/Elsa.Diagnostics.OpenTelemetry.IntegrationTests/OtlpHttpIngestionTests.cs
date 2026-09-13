using System.Net;
using System.Text;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OtlpHttpIngestionTests : OpenTelemetryWebApplicationTest
{
    private static readonly byte[] TraceId = Convert.FromHexString("00112233445566778899aabbccddeeff");
    private static readonly byte[] SpanId = Convert.FromHexString("0011223344556677");
    private static readonly byte[] ChildSpanId = Convert.FromHexString("8899aabbccddeeff");
    private static readonly DateTimeOffset Timestamp = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task PostTraces_WhenPayloadIsValid_StoresQueryableTrace()
    {
        using var client = Factory.CreateClient();
        using var content = new ByteArrayContent(CreateTracePayload());
        content.Headers.ContentType = new("application/x-protobuf");

        using var response = await client.PostAsync("/elsa/otlp/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var provider = Services.GetRequiredService<IOpenTelemetryProvider>();
        var result = await provider.GetTracesAsync(new OpenTelemetryTraceFilter { TraceId = "00112233445566778899aabbccddeeff" });
        var trace = await Assert.That(result.Items).HasSingleItem();

        await Assert.That(trace.Name).IsEqualTo("Workflow/Approve");
        var workflowInstanceId = await Assert.That(trace.WorkflowInstanceIds).HasSingleItem();
        await Assert.That(workflowInstanceId).IsEqualTo("wf-1");

        var detail = await Assert.That(await provider.GetTraceAsync(trace.TraceId)).IsNotNull();

        await Assert.That(detail.Spans.Count).IsEqualTo(2);

        var rootSpan = await Assert.That(detail.Spans).HasSingleItem(x => x.SpanId == "0011223344556677");
        await Assert.That(rootSpan.Attributes["workflow.definition.id"]).IsEqualTo("order-workflow");
        await Assert.That(rootSpan.Attributes["workflow.instance.id"]).IsEqualTo("wf-1");

        var activitySpan = await Assert.That(detail.Spans).HasSingleItem(x => x.SpanId == "8899aabbccddeeff");
        await Assert.That(activitySpan.ParentSpanId).IsEqualTo("0011223344556677");
        await Assert.That(activitySpan.Attributes["activity.id"]).IsEqualTo("approve-task");
        await Assert.That(activitySpan.Attributes["activity.node.id"]).IsEqualTo("node-approve");
    }

    [Test]
    public async Task PostTraces_WhenPayloadExceedsConfiguredLimit_ReturnsPayloadTooLarge()
    {
        Services.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value.MaxHttpRequestBodySize = 1;
        using var client = Factory.CreateClient();
        using var content = new ByteArrayContent(CreateTracePayload());
        content.Headers.ContentType = new("application/x-protobuf");

        using var response = await client.PostAsync("/elsa/otlp/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task PostTraces_WhenPayloadIsTruncated_ReturnsBadRequest()
    {
        using var client = Factory.CreateClient();
        using var content = new ByteArrayContent([0x0a, 0x04, 0x08]);
        content.Headers.ContentType = new("application/x-protobuf");

        using var response = await client.PostAsync("/elsa/otlp/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    private static byte[] CreateTracePayload()
    {
        return Message(1,
            Join(
                Message(1, Resource()),
                Message(2,
                    Join(
                        Message(2,
                            Join(
                                Bytes(1, TraceId),
                                Bytes(2, SpanId),
                                String(5, "Workflow/Approve"),
                                Varint(6, 1),
                                Varint(7, UnixNanos(Timestamp)),
                                Varint(8, UnixNanos(Timestamp.AddMilliseconds(25))),
                                Message(9, KeyValue("workflow.instance.id", "wf-1")),
                                Message(9, KeyValue("workflow.definition.id", "order-workflow")),
                                Message(15, Varint(3, 1)))),
                        Message(2,
                            Join(
                                Bytes(1, TraceId),
                                Bytes(2, ChildSpanId),
                                Bytes(4, SpanId),
                                String(5, "Activity/ApproveTask"),
                                Varint(6, 1),
                                Varint(7, UnixNanos(Timestamp.AddMilliseconds(5))),
                                Varint(8, UnixNanos(Timestamp.AddMilliseconds(20))),
                                Message(9, KeyValue("workflow.instance.id", "wf-1")),
                                Message(9, KeyValue("activity.id", "approve-task")),
                                Message(9, KeyValue("activity.node.id", "node-approve")),
                                Message(15, Varint(3, 1))))))));
    }

    private static byte[] Resource()
    {
        return Join(
            Message(1, KeyValue("service.name", "elsa-server")),
            Message(1, KeyValue("service.instance.id", "node-1")),
            Message(1, KeyValue("telemetry.sdk.language", "dotnet")));
    }

    private static byte[] KeyValue(string key, string value) => Join(String(1, key), Message(2, String(1, value)));

    private static byte[] Message(int fieldNumber, byte[] value) => Join(Varint((ulong)((fieldNumber << 3) | 2)), Varint((ulong)value.Length), value);

    private static byte[] String(int fieldNumber, string value) => Message(fieldNumber, Encoding.UTF8.GetBytes(value));

    private static byte[] Bytes(int fieldNumber, byte[] value) => Message(fieldNumber, value);

    private static byte[] Varint(int fieldNumber, ulong value) => Join(Varint((ulong)(fieldNumber << 3)), Varint(value));

    private static byte[] Varint(ulong value)
    {
        var bytes = new List<byte>();
        while (value >= 0x80)
        {
            bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }
        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    private static ulong UnixNanos(DateTimeOffset timestamp) => (ulong)(timestamp - DateTimeOffset.UnixEpoch).Ticks * 100;

    private static byte[] Join(params byte[][] segments)
    {
        var result = new byte[segments.Sum(x => x.Length)];
        var offset = 0;
        foreach (var segment in segments)
        {
            segment.CopyTo(result, offset);
            offset += segment.Length;
        }

        return result;
    }
}
