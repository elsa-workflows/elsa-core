using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Models;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class RelationalStructuredLogMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RelationalStructuredLogRecord Map(StructuredLogEvent logEvent)
    {
        return new()
        {
            Id = logEvent.Id,
            Sequence = logEvent.Sequence,
            Timestamp = FormatTimestamp(logEvent.Timestamp),
            ReceivedAt = FormatTimestamp(logEvent.ReceivedAt),
            Level = logEvent.Level,
            Category = logEvent.Category,
            EventId = logEvent.EventId,
            EventName = logEvent.EventName,
            Message = logEvent.Message,
            MessageTemplate = logEvent.MessageTemplate,
            ExceptionJson = logEvent.Exception == null ? null : JsonSerializer.Serialize(logEvent.Exception, JsonOptions),
            ScopesJson = JsonSerializer.Serialize(logEvent.Scopes, JsonOptions),
            PropertiesJson = JsonSerializer.Serialize(logEvent.Properties, JsonOptions),
            TraceId = logEvent.TraceId,
            SpanId = logEvent.SpanId,
            CorrelationId = logEvent.CorrelationId,
            TenantId = logEvent.TenantId,
            WorkflowDefinitionId = logEvent.WorkflowDefinitionId,
            WorkflowInstanceId = logEvent.WorkflowInstanceId,
            SourceId = logEvent.SourceId
        };
    }

    public StructuredLogEvent Map(DbDataReader reader)
    {
        return new()
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Sequence = reader.GetInt64(reader.GetOrdinal("Sequence")),
            Timestamp = ParseTimestamp(reader.GetString(reader.GetOrdinal("Timestamp"))),
            ReceivedAt = ParseTimestamp(reader.GetString(reader.GetOrdinal("ReceivedAt"))),
            Level = (StructuredLogLevel)reader.GetInt32(reader.GetOrdinal("Level")),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
            EventName = ReadNullableString(reader, "EventName"),
            Message = reader.GetString(reader.GetOrdinal("Message")),
            MessageTemplate = ReadNullableString(reader, "MessageTemplate"),
            Exception = Deserialize<StructuredLogException>(ReadNullableString(reader, "ExceptionJson")),
            Scopes = DeserializeDictionary(ReadNullableString(reader, "ScopesJson")),
            Properties = DeserializeDictionary(ReadNullableString(reader, "PropertiesJson")),
            TraceId = ReadNullableString(reader, "TraceId"),
            SpanId = ReadNullableString(reader, "SpanId"),
            CorrelationId = ReadNullableString(reader, "CorrelationId"),
            TenantId = ReadNullableString(reader, "TenantId"),
            WorkflowDefinitionId = ReadNullableString(reader, "WorkflowDefinitionId"),
            WorkflowInstanceId = ReadNullableString(reader, "WorkflowInstanceId"),
            SourceId = reader.GetString(reader.GetOrdinal("SourceId"))
        };
    }

    public static string FormatTimestamp(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset ParseTimestamp(string timestamp)
    {
        return DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static string? ReadNullableString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static IDictionary<string, string?> DeserializeDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string?>();

        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions) ?? new Dictionary<string, string?>();
    }

    private static T? Deserialize<T>(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
