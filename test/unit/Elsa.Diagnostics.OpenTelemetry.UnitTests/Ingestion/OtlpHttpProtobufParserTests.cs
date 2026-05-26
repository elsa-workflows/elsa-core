using System.Buffers.Binary;
using System.Text;
using Elsa.Diagnostics.OpenTelemetry.Ingestion.HttpProtobuf;
using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.UnitTests.Ingestion;

public class OtlpHttpProtobufParserTests
{
    private static readonly byte[] TraceId = Convert.FromHexString("00112233445566778899aabbccddeeff");
    private static readonly byte[] SpanId = Convert.FromHexString("0011223344556677");
    private static readonly DateTimeOffset Timestamp = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "OTLP trace payload is normalized to trace, span, resource, and workflow metadata")]
    public void ParseTraces()
    {
        var payload = Message(1,
            Join(Message(1, Resource()),
            Message(2, Message(2,
                Join(Bytes(1, TraceId),
                Bytes(2, SpanId),
                String(5, "Workflow/Approve"),
                Varint(6, 1),
                Varint(7, UnixNanos(Timestamp)),
                Varint(8, UnixNanos(Timestamp.AddMilliseconds(25))),
                Message(9, KeyValue("workflow.instance.id", "wf-1")),
                Message(15, Varint(3, 2)))))));

        var batch = OtlpHttpProtobufParser.ParseTraces(payload);
        var span = Assert.Single(batch.Spans);
        var trace = Assert.Single(batch.Traces);

        Assert.Equal("elsa-server:node-1", span.ResourceId);
        Assert.Equal("00112233445566778899aabbccddeeff", trace.TraceId);
        Assert.Equal("0011223344556677", trace.RootSpanId);
        Assert.Equal("wf-1", Assert.Single(trace.WorkflowInstanceIds));
        Assert.Equal(SpanStatus.Error, trace.Status);
    }

    [Fact(DisplayName = "OTLP metric payload is normalized to metric instruments and points")]
    public void ParseMetrics()
    {
        var point =
            Join(Varint(3, UnixNanos(Timestamp)),
            Fixed64(4, 42.5),
            Message(7, KeyValue("workflow.definition.id", "orders")));
        var metric =
            Join(String(1, "workflow.duration"),
            String(2, "Workflow duration"),
            String(3, "ms"),
            Message(5, Message(1, point)));
        var payload = Message(1, Join(Message(1, Resource()), Message(2, Message(2, metric))));

        var batch = OtlpHttpProtobufParser.ParseMetrics(payload);
        var instrument = Assert.Single(batch.Instruments);
        var metricPoint = Assert.Single(batch.MetricPoints);

        Assert.Equal("workflow.duration", instrument.Name);
        Assert.Equal(MetricKind.Gauge, instrument.Kind);
        Assert.Equal(42.5, metricPoint.Value);
        Assert.Equal("orders", metricPoint.Attributes["workflow.definition.id"]);
    }

    [Fact(DisplayName = "OTLP log payload is normalized to correlated OTLP log records")]
    public void ParseLogs()
    {
        var log =
            Join(Varint(1, UnixNanos(Timestamp)),
            Varint(2, 17),
            String(3, "Error"),
            Message(5, AnyString("boom")),
            Bytes(9, TraceId),
            Bytes(10, SpanId),
            Message(6, KeyValue("workflow.instance.id", "wf-1")));
        var payload = Message(1, Join(Message(1, Resource()), Message(2, Message(2, log))));

        var batch = OtlpHttpProtobufParser.ParseLogs(payload);
        var record = Assert.Single(batch.Logs);

        Assert.Equal("elsa-server:node-1", record.ResourceId);
        Assert.Equal("Error", record.SeverityText);
        Assert.Equal("boom", record.Body);
        Assert.Equal("00112233445566778899aabbccddeeff", record.TraceId);
        Assert.Equal("0011223344556677", record.SpanId);
    }

    private static byte[] Resource()
    {
        return Join(
            Message(1, KeyValue("service.name", "elsa-server")),
            Message(1, KeyValue("service.instance.id", "node-1")),
            Message(1, KeyValue("telemetry.sdk.language", "dotnet")));
    }

    private static byte[] KeyValue(string key, string value) => Join(String(1, key), Message(2, AnyString(value)));

    private static byte[] AnyString(string value) => String(1, value);

    private static byte[] Message(int fieldNumber, byte[] value) => Join(Varint((ulong)((fieldNumber << 3) | 2)), Varint((ulong)value.Length), value);

    private static byte[] String(int fieldNumber, string value) => Message(fieldNumber, Encoding.UTF8.GetBytes(value));

    private static byte[] Bytes(int fieldNumber, byte[] value) => Message(fieldNumber, value);

    private static byte[] Varint(int fieldNumber, ulong value) => Join(Varint((ulong)(fieldNumber << 3)), Varint(value));

    private static byte[] Fixed64(int fieldNumber, double value)
    {
        var bytes = new byte[9];
        bytes[0] = (byte)((fieldNumber << 3) | 1);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(1), (ulong)BitConverter.DoubleToInt64Bits(value));
        return bytes;
    }

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
