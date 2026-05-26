using System.Buffers.Binary;
using System.Net;
using System.Text;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.OpenTelemetry.IntegrationTests;

public class OtlpHttpIngestionTests : IAsyncLifetime
{
    private static readonly byte[] TraceId = Convert.FromHexString("00112233445566778899aabbccddeeff");
    private static readonly byte[] SpanId = Convert.FromHexString("0011223344556677");
    private static readonly byte[] ChildSpanId = Convert.FromHexString("8899aabbccddeeff");
    private static readonly DateTimeOffset Timestamp = new(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);

    private WebApplication? _app;
    private HttpClient _httpClient = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenTelemetryDiagnosticsServices(options =>
        {
            options.AllowUnauthenticatedLoopback = true;
            options.HttpEndpointPath = "/elsa/otlp/v1";
        });

        _app = builder.Build();
        _app.MapOpenTelemetryHttpProtobufCollector();

        await _app.StartAsync();
        _httpClient = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();

        if (_app == null)
            return;

        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task PostTraces_WhenPayloadIsValid_StoresQueryableTrace()
    {
        using var content = new ByteArrayContent(CreateTracePayload());
        content.Headers.ContentType = new("application/x-protobuf");

        var response = await _httpClient.PostAsync("/elsa/otlp/v1/traces", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var provider = _app!.Services.GetRequiredService<IOpenTelemetryProvider>();
        var result = await provider.GetTracesAsync(new OpenTelemetryTraceFilter { TraceId = "00112233445566778899aabbccddeeff" });
        var trace = Assert.Single(result.Items);

        Assert.Equal("Workflow/Approve", trace.Name);
        Assert.Equal("wf-1", Assert.Single(trace.WorkflowInstanceIds));

        var detail = await provider.GetTraceAsync(trace.TraceId);

        Assert.NotNull(detail);
        Assert.Equal(2, detail.Spans.Count);

        var rootSpan = Assert.Single(detail.Spans, x => x.SpanId == "0011223344556677");
        Assert.Equal("order-workflow", rootSpan.Attributes["workflow.definition.id"]);
        Assert.Equal("wf-1", rootSpan.Attributes["workflow.instance.id"]);

        var activitySpan = Assert.Single(detail.Spans, x => x.SpanId == "8899aabbccddeeff");
        Assert.Equal("0011223344556677", activitySpan.ParentSpanId);
        Assert.Equal("approve-task", activitySpan.Attributes["activity.id"]);
        Assert.Equal("node-approve", activitySpan.Attributes["activity.node.id"]);
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
