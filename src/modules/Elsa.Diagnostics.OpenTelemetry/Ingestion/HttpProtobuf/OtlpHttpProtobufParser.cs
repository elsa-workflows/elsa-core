using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Ingestion.HttpProtobuf;

internal static class OtlpHttpProtobufParser
{
    public static OpenTelemetryBatch ParseTraces(ReadOnlySpan<byte> payload)
    {
        var resources = new List<TelemetryResource>();
        var spans = new List<TelemetrySpan>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number != 1 || field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            var resourceSpans = ParseResourceSpans(field.Bytes);
            resources.Add(resourceSpans.Resource);
            spans.AddRange(resourceSpans.Spans);
        }

        return CreateBatch(resources, spans, [], [], []);
    }

    public static OpenTelemetryBatch ParseMetrics(ReadOnlySpan<byte> payload)
    {
        var resources = new List<TelemetryResource>();
        var instruments = new Dictionary<string, MetricInstrument>(StringComparer.OrdinalIgnoreCase);
        var points = new List<MetricPoint>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number != 1 || field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            var resourceMetrics = ParseResourceMetrics(field.Bytes);
            resources.Add(resourceMetrics.Resource);

            foreach (var instrument in resourceMetrics.Instruments)
                instruments[instrument.Id] = instrument;

            points.AddRange(resourceMetrics.Points);
        }

        return CreateBatch(resources, [], instruments.Values.ToList(), points, []);
    }

    public static OpenTelemetryBatch ParseLogs(ReadOnlySpan<byte> payload)
    {
        var resources = new List<TelemetryResource>();
        var logs = new List<OtlpLogRecord>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number != 1 || field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            var resourceLogs = ParseResourceLogs(field.Bytes);
            resources.Add(resourceLogs.Resource);
            logs.AddRange(resourceLogs.Logs);
        }

        return CreateBatch(resources, [], [], [], logs);
    }

    private static (TelemetryResource Resource, List<TelemetrySpan> Spans) ParseResourceSpans(ReadOnlySpan<byte> payload)
    {
        var resource = CreateResource(new Dictionary<string, string?>());
        var spans = new List<TelemetrySpan>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            if (field.Number == 1)
                resource = CreateResource(ParseResourceAttributes(field.Bytes));
            else if (field.Number == 2)
                spans.AddRange(ParseScopeSpans(field.Bytes, resource.Id));
        }

        return (resource, spans);
    }

    private static List<TelemetrySpan> ParseScopeSpans(ReadOnlySpan<byte> payload, string resourceId)
    {
        var spans = new List<TelemetrySpan>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                spans.Add(ParseSpan(field.Bytes, resourceId));
        }

        return spans;
    }

    private static TelemetrySpan ParseSpan(ReadOnlySpan<byte> payload, string resourceId)
    {
        var traceId = "";
        var spanId = "";
        string? parentSpanId = null;
        var name = "";
        var kind = "unspecified";
        var start = DateTimeOffset.UnixEpoch;
        var end = DateTimeOffset.UnixEpoch;
        var status = SpanStatus.Unset;
        string? statusDescription = null;
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var events = new List<TelemetrySpanEvent>();
        var links = new List<TelemetrySpanLink>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            switch (field.Number)
            {
                case 1 when field.WireType == ProtobufWireType.LengthDelimited:
                    traceId = ToHex(field.Bytes);
                    break;
                case 2 when field.WireType == ProtobufWireType.LengthDelimited:
                    spanId = ToHex(field.Bytes);
                    break;
                case 4 when field.WireType == ProtobufWireType.LengthDelimited:
                    parentSpanId = ToHex(field.Bytes);
                    break;
                case 5 when field.WireType == ProtobufWireType.LengthDelimited:
                    name = field.StringValue();
                    break;
                case 6:
                    kind = SpanKindName(field.Varint);
                    break;
                case 7:
                    start = FromUnixNanos(field.Varint);
                    break;
                case 8:
                    end = FromUnixNanos(field.Varint);
                    break;
                case 9 when field.WireType == ProtobufWireType.LengthDelimited:
                    AddAttribute(attributes, field.Bytes);
                    break;
                case 11 when field.WireType == ProtobufWireType.LengthDelimited:
                    events.Add(ParseSpanEvent(field.Bytes));
                    break;
                case 12 when field.WireType == ProtobufWireType.LengthDelimited:
                    links.Add(ParseSpanLink(field.Bytes));
                    break;
                case 15 when field.WireType == ProtobufWireType.LengthDelimited:
                    (status, statusDescription) = ParseSpanStatus(field.Bytes);
                    break;
            }
        }

        if (end < start)
            end = start;

        return new TelemetrySpan($"{traceId}:{spanId}", traceId, spanId, parentSpanId, resourceId, name, kind, start, end, status, statusDescription, attributes, events, links);
    }

    private static TelemetrySpanEvent ParseSpanEvent(ReadOnlySpan<byte> payload)
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        var name = "";
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1)
                timestamp = FromUnixNanos(field.Varint);
            else if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                name = field.StringValue();
            else if (field.Number == 3 && field.WireType == ProtobufWireType.LengthDelimited)
                AddAttribute(attributes, field.Bytes);
        }

        return new TelemetrySpanEvent(name, timestamp, attributes);
    }

    private static TelemetrySpanLink ParseSpanLink(ReadOnlySpan<byte> payload)
    {
        var traceId = "";
        var spanId = "";
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1 && field.WireType == ProtobufWireType.LengthDelimited)
                traceId = ToHex(field.Bytes);
            else if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                spanId = ToHex(field.Bytes);
            else if (field.Number == 3 && field.WireType == ProtobufWireType.LengthDelimited)
                AddAttribute(attributes, field.Bytes);
        }

        return new TelemetrySpanLink(traceId, spanId, attributes);
    }

    private static (SpanStatus Status, string? Description) ParseSpanStatus(ReadOnlySpan<byte> payload)
    {
        var status = SpanStatus.Unset;
        string? description = null;
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                description = field.StringValue();
            else if (field.Number == 3)
                status = field.Varint switch
                {
                    1 => SpanStatus.Ok,
                    2 => SpanStatus.Error,
                    _ => SpanStatus.Unset
                };
        }

        return (status, description);
    }

    private static (TelemetryResource Resource, List<MetricInstrument> Instruments, List<MetricPoint> Points) ParseResourceMetrics(ReadOnlySpan<byte> payload)
    {
        var resource = CreateResource(new Dictionary<string, string?>());
        var instruments = new List<MetricInstrument>();
        var points = new List<MetricPoint>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            if (field.Number == 1)
                resource = CreateResource(ParseResourceAttributes(field.Bytes));
            else if (field.Number == 2)
                ParseScopeMetrics(field.Bytes, resource.Id, instruments, points);
        }

        return (resource, instruments, points);
    }

    private static void ParseScopeMetrics(ReadOnlySpan<byte> payload, string resourceId, ICollection<MetricInstrument> instruments, ICollection<MetricPoint> points)
    {
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number != 2 || field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            var metric = ParseMetric(field.Bytes, resourceId);
            instruments.Add(metric.Instrument);

            foreach (var point in metric.Points)
                points.Add(point);
        }
    }

    private static (MetricInstrument Instrument, List<MetricPoint> Points) ParseMetric(ReadOnlySpan<byte> payload, string resourceId)
    {
        var name = "";
        string? description = null;
        string? unit = null;
        var kind = MetricKind.Gauge;
        var points = new List<MetricPoint>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            switch (field.Number)
            {
                case 1 when field.WireType == ProtobufWireType.LengthDelimited:
                    name = field.StringValue();
                    break;
                case 2 when field.WireType == ProtobufWireType.LengthDelimited:
                    description = field.StringValue();
                    break;
                case 3 when field.WireType == ProtobufWireType.LengthDelimited:
                    unit = field.StringValue();
                    break;
                case 5 when field.WireType == ProtobufWireType.LengthDelimited:
                    kind = MetricKind.Gauge;
                    points.AddRange(ParseNumberDataPoints(field.Bytes, resourceId, () => $"{resourceId}:{name}:gauge"));
                    break;
                case 7 when field.WireType == ProtobufWireType.LengthDelimited:
                    kind = MetricKind.Sum;
                    points.AddRange(ParseNumberDataPoints(field.Bytes, resourceId, () => $"{resourceId}:{name}:sum"));
                    break;
                case 9 when field.WireType == ProtobufWireType.LengthDelimited:
                    kind = MetricKind.Histogram;
                    points.AddRange(ParseHistogramDataPoints(field.Bytes, resourceId, () => $"{resourceId}:{name}:histogram"));
                    break;
            }
        }

        var instrumentId = $"{resourceId}:{name}:{kind.ToString().ToLowerInvariant()}";
        var instrument = new MetricInstrument(instrumentId, resourceId, name, unit, description, kind, new Dictionary<string, string?>());
        points = points.Select(x => x with { InstrumentId = instrumentId, InstrumentName = name }).ToList();
        return (instrument, points);
    }

    private static List<MetricPoint> ParseNumberDataPoints(ReadOnlySpan<byte> payload, string resourceId, Func<string> instrumentIdFactory)
    {
        var points = new List<MetricPoint>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1 && field.WireType == ProtobufWireType.LengthDelimited)
                points.Add(ParseNumberDataPoint(field.Bytes, resourceId, instrumentIdFactory()));
        }

        return points;
    }

    private static MetricPoint ParseNumberDataPoint(ReadOnlySpan<byte> payload, string resourceId, string instrumentId)
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        double? value = null;
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 3)
                timestamp = FromUnixNanos(field.Varint);
            else if (field.Number == 4 && field.WireType == ProtobufWireType.Fixed64)
                value = field.DoubleValue;
            else if (field.Number == 6)
                value = (long)field.Varint;
            else if (field.Number == 7 && field.WireType == ProtobufWireType.LengthDelimited)
                AddAttribute(attributes, field.Bytes);
        }

        return new MetricPoint(Guid.NewGuid().ToString("N"), instrumentId, "", resourceId, timestamp, value, null, null, attributes, null, null);
    }

    private static List<MetricPoint> ParseHistogramDataPoints(ReadOnlySpan<byte> payload, string resourceId, Func<string> instrumentIdFactory)
    {
        var points = new List<MetricPoint>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1 && field.WireType == ProtobufWireType.LengthDelimited)
                points.Add(ParseHistogramDataPoint(field.Bytes, resourceId, instrumentIdFactory()));
        }

        return points;
    }

    private static MetricPoint ParseHistogramDataPoint(ReadOnlySpan<byte> payload, string resourceId, string instrumentId)
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        long? count = null;
        double? sum = null;
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 3)
                timestamp = FromUnixNanos(field.Varint);
            else if (field.Number == 4)
                count = (long)field.Varint;
            else if (field.Number == 5 && field.WireType == ProtobufWireType.Fixed64)
                sum = field.DoubleValue;
            else if (field.Number == 9 && field.WireType == ProtobufWireType.LengthDelimited)
                AddAttribute(attributes, field.Bytes);
        }

        return new MetricPoint(Guid.NewGuid().ToString("N"), instrumentId, "", resourceId, timestamp, null, sum, count, attributes, null, null);
    }

    private static (TelemetryResource Resource, List<OtlpLogRecord> Logs) ParseResourceLogs(ReadOnlySpan<byte> payload)
    {
        var resource = CreateResource(new Dictionary<string, string?>());
        var logs = new List<OtlpLogRecord>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.WireType != ProtobufWireType.LengthDelimited)
                continue;

            if (field.Number == 1)
                resource = CreateResource(ParseResourceAttributes(field.Bytes));
            else if (field.Number == 2)
                logs.AddRange(ParseScopeLogs(field.Bytes, resource.Id));
        }

        return (resource, logs);
    }

    private static List<OtlpLogRecord> ParseScopeLogs(ReadOnlySpan<byte> payload, string resourceId)
    {
        var logs = new List<OtlpLogRecord>();
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                logs.Add(ParseLogRecord(field.Bytes, resourceId));
        }

        return logs;
    }

    private static OtlpLogRecord ParseLogRecord(ReadOnlySpan<byte> payload, string resourceId)
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        var severityText = "";
        int? severityNumber = null;
        var body = "";
        string? traceId = null;
        string? spanId = null;
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            switch (field.Number)
            {
                case 1:
                    timestamp = FromUnixNanos(field.Varint);
                    break;
                case 2:
                    severityNumber = (int)field.Varint;
                    break;
                case 3 when field.WireType == ProtobufWireType.LengthDelimited:
                    severityText = field.StringValue();
                    break;
                case 5 when field.WireType == ProtobufWireType.LengthDelimited:
                    body = ParseAnyValue(field.Bytes) ?? "";
                    break;
                case 6 when field.WireType == ProtobufWireType.LengthDelimited:
                    AddAttribute(attributes, field.Bytes);
                    break;
                case 9 when field.WireType == ProtobufWireType.LengthDelimited:
                    traceId = ToHex(field.Bytes);
                    break;
                case 10 when field.WireType == ProtobufWireType.LengthDelimited:
                    spanId = ToHex(field.Bytes);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(severityText) && severityNumber != null)
            severityText = SeverityName(severityNumber.Value);

        return new OtlpLogRecord(Guid.NewGuid().ToString("N"), resourceId, timestamp, severityText, severityNumber, body, traceId, spanId, attributes);
    }

    private static Dictionary<string, string?> ParseResourceAttributes(ReadOnlySpan<byte> payload)
    {
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1 && field.WireType == ProtobufWireType.LengthDelimited)
                AddAttribute(attributes, field.Bytes);
        }

        return attributes;
    }

    private static void AddAttribute(IDictionary<string, string?> attributes, ReadOnlySpan<byte> payload)
    {
        string? key = null;
        string? value = null;
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            if (field.Number == 1 && field.WireType == ProtobufWireType.LengthDelimited)
                key = field.StringValue();
            else if (field.Number == 2 && field.WireType == ProtobufWireType.LengthDelimited)
                value = ParseAnyValue(field.Bytes);
        }

        if (!string.IsNullOrWhiteSpace(key))
            attributes[key] = value;
    }

    private static string? ParseAnyValue(ReadOnlySpan<byte> payload)
    {
        var reader = new ProtobufReader(payload);

        while (reader.TryReadField(out var field))
        {
            return field.Number switch
            {
                1 when field.WireType == ProtobufWireType.LengthDelimited => field.StringValue(),
                2 => field.Varint != 0 ? "true" : "false",
                3 => ((long)field.Varint).ToString(CultureInfo.InvariantCulture),
                4 when field.WireType == ProtobufWireType.Fixed64 => field.DoubleValue.ToString(CultureInfo.InvariantCulture),
                7 when field.WireType == ProtobufWireType.LengthDelimited => ToHex(field.Bytes),
                _ => null
            };
        }

        return null;
    }

    private static TelemetryResource CreateResource(IDictionary<string, string?> attributes)
    {
        var serviceName = GetAttribute(attributes, "service.name") ?? "unknown_service";
        var instanceId = GetAttribute(attributes, "service.instance.id");
        var resourceId = string.IsNullOrWhiteSpace(instanceId) ? serviceName : $"{serviceName}:{instanceId}";
        return new TelemetryResource(resourceId, serviceName, instanceId, GetAttribute(attributes, "telemetry.sdk.language"), new Dictionary<string, string?>(attributes, StringComparer.OrdinalIgnoreCase), DateTimeOffset.UtcNow, TelemetryResourceStatus.Active);
    }

    private static OpenTelemetryBatch CreateBatch(
        IReadOnlyCollection<TelemetryResource> resources,
        IReadOnlyCollection<TelemetrySpan> spans,
        IReadOnlyCollection<MetricInstrument> instruments,
        IReadOnlyCollection<MetricPoint> points,
        IReadOnlyCollection<OtlpLogRecord> logs)
    {
        var traces = spans
            .GroupBy(x => x.TraceId, StringComparer.OrdinalIgnoreCase)
            .Select(CreateTrace)
            .ToList();

        return new OpenTelemetryBatch(resources.DistinctBy(x => x.Id).ToList(), traces, spans.ToList(), instruments.ToList(), points.ToList(), logs.ToList());
    }

    private static TelemetryTrace CreateTrace(IGrouping<string, TelemetrySpan> spans)
    {
        var orderedSpans = spans.OrderBy(x => x.StartTime).ToList();
        var root = orderedSpans.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.ParentSpanId)) ?? orderedSpans[0];
        var start = orderedSpans.Min(x => x.StartTime);
        var end = orderedSpans.Max(x => x.EndTime);
        var status = orderedSpans.Any(x => x.Status == SpanStatus.Error) ? SpanStatus.Error : orderedSpans.Any(x => x.Status == SpanStatus.Ok) ? SpanStatus.Ok : SpanStatus.Unset;
        var workflowInstanceIds = orderedSpans
            .Select(x => GetAttribute(x.Attributes, "workflow.instance.id"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TelemetryTrace(spans.Key, root.SpanId, root.Name, start, end, end - start, status, orderedSpans.Select(x => x.ResourceId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), workflowInstanceIds, orderedSpans.Count);
    }

    private static string? GetAttribute(IDictionary<string, string?> attributes, string key) => attributes.TryGetValue(key, out var value) ? value : null;

    private static DateTimeOffset FromUnixNanos(ulong value)
    {
        var ticks = checked((long)(value / 100));
        return DateTimeOffset.UnixEpoch.AddTicks(ticks);
    }

    private static string ToHex(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string SpanKindName(ulong value) => value switch
    {
        1 => "internal",
        2 => "server",
        3 => "client",
        4 => "producer",
        5 => "consumer",
        _ => "unspecified"
    };

    private static string SeverityName(int value) => value switch
    {
        >= 21 => "Fatal",
        >= 17 => "Error",
        >= 13 => "Warning",
        >= 9 => "Information",
        >= 5 => "Debug",
        >= 1 => "Trace",
        _ => "Unspecified"
    };

    private enum ProtobufWireType
    {
        Varint = 0,
        Fixed64 = 1,
        LengthDelimited = 2,
        Fixed32 = 5
    }

    private readonly ref struct ProtobufField
    {
        public ProtobufField(int number, ProtobufWireType wireType, ulong varint, ReadOnlySpan<byte> bytes, double doubleValue)
        {
            Number = number;
            WireType = wireType;
            Varint = varint;
            Bytes = bytes;
            DoubleValue = doubleValue;
        }

        public int Number { get; }
        public ProtobufWireType WireType { get; }
        public ulong Varint { get; }
        public ReadOnlySpan<byte> Bytes { get; }
        public double DoubleValue { get; }

        public string StringValue() => Encoding.UTF8.GetString(Bytes);
    }

    private ref struct ProtobufReader
    {
        private ReadOnlySpan<byte> _remaining;

        public ProtobufReader(ReadOnlySpan<byte> payload)
        {
            _remaining = payload;
        }

        public bool TryReadField(out ProtobufField field)
        {
            field = default;

            if (_remaining.IsEmpty)
                return false;

            var tag = ReadVarint();
            var number = (int)(tag >> 3);
            var wireType = (ProtobufWireType)(tag & 7);

            switch (wireType)
            {
                case ProtobufWireType.Varint:
                    field = new ProtobufField(number, wireType, ReadVarint(), default, default);
                    return true;
                case ProtobufWireType.Fixed64:
                    EnsureAvailable(8);
                    var fixed64 = BinaryPrimitives.ReadUInt64LittleEndian(_remaining[..8]);
                    _remaining = _remaining[8..];
                    field = new ProtobufField(number, wireType, fixed64, default, BitConverter.Int64BitsToDouble((long)fixed64));
                    return true;
                case ProtobufWireType.LengthDelimited:
                    var length = checked((int)ReadVarint());
                    EnsureAvailable(length);
                    var bytes = _remaining[..length];
                    _remaining = _remaining[length..];
                    field = new ProtobufField(number, wireType, default, bytes, default);
                    return true;
                case ProtobufWireType.Fixed32:
                    EnsureAvailable(4);
                    _remaining = _remaining[4..];
                    field = new ProtobufField(number, wireType, default, default, default);
                    return true;
                default:
                    throw new InvalidDataException($"Unsupported protobuf wire type '{wireType}'.");
            }
        }

        private ulong ReadVarint()
        {
            ulong value = 0;
            var shift = 0;

            for (var i = 0; i < 10; i++)
            {
                if (_remaining.IsEmpty)
                    throw new InvalidDataException("Unexpected end of protobuf payload.");

                var b = _remaining[0];
                _remaining = _remaining[1..];
                value |= (ulong)(b & 0x7f) << shift;

                if ((b & 0x80) == 0)
                    return value;

                shift += 7;
            }

            throw new InvalidDataException("Invalid protobuf varint.");
        }

        private readonly void EnsureAvailable(int byteCount)
        {
            if (_remaining.Length < byteCount)
                throw new InvalidDataException("Unexpected end of protobuf payload.");
        }
    }
}
