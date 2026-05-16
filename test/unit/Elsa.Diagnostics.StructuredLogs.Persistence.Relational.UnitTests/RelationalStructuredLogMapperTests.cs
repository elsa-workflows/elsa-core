using System.Data;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogMapperTests
{
    private readonly RelationalStructuredLogMapper _mapper = new();

    [Fact]
    public void Map_SerializesJsonFields()
    {
        var logEvent = new StructuredLogEvent
        {
            Id = "event-a",
            Timestamp = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            Level = StructuredLogLevel.Error,
            Category = "Elsa.Tests",
            Message = "Failed",
            Exception = new("System.Exception", "Boom", "Stack"),
            SourceId = "source-a",
            Scopes = new Dictionary<string, string?> { ["Scope"] = "Value" },
            Properties = new Dictionary<string, string?> { ["Property"] = "Value" }
        };

        var record = _mapper.Map(logEvent);

        Assert.Contains("Boom", record.ExceptionJson, StringComparison.Ordinal);
        Assert.Contains("Scope", record.ScopesJson, StringComparison.Ordinal);
        Assert.Contains("Property", record.PropertiesJson, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatTimestamp_StoresUtcIso8601Text()
    {
        var timestamp = new DateTimeOffset(2026, 5, 13, 15, 0, 0, TimeSpan.FromHours(2));

        var formatted = RelationalStructuredLogMapper.FormatTimestamp(timestamp);

        Assert.Equal("2026-05-13T13:00:00.0000000+00:00", formatted);
        Assert.Equal(timestamp.ToUniversalTime(), RelationalStructuredLogMapper.ParseTimestamp(formatted));
    }

    [Fact]
    public void MapReader_DeserializesStoredJsonAndNullableFields()
    {
        var timestamp = new DateTimeOffset(2026, 5, 13, 15, 0, 0, TimeSpan.FromHours(2));
        var logEvent = new StructuredLogEvent
        {
            Id = "event-a",
            Sequence = 123,
            Timestamp = timestamp,
            ReceivedAt = timestamp.AddSeconds(1),
            Level = StructuredLogLevel.Warning,
            Category = "Elsa.Tests",
            EventId = 42,
            EventName = "TestEvent",
            Message = "Message",
            MessageTemplate = "Message {Value}",
            Exception = new("System.InvalidOperationException", "Boom", "Stack"),
            Scopes = new Dictionary<string, string?> { ["Scope"] = "Value" },
            Properties = new Dictionary<string, string?> { ["Property"] = "Value" },
            TraceId = "trace-a",
            SpanId = "span-a",
            CorrelationId = "correlation-a",
            TenantId = "tenant-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            SourceId = "source-a"
        };

        using var reader = CreateReader(_mapper.Map(logEvent));
        Assert.True(reader.Read());

        var mapped = _mapper.Map(reader);

        Assert.Equal(logEvent.Id, mapped.Id);
        Assert.Equal(logEvent.Sequence, mapped.Sequence);
        Assert.Equal(logEvent.Timestamp.ToUniversalTime(), mapped.Timestamp);
        Assert.Equal(logEvent.ReceivedAt.ToUniversalTime(), mapped.ReceivedAt);
        Assert.Equal(logEvent.Level, mapped.Level);
        Assert.Equal(logEvent.Category, mapped.Category);
        Assert.Equal(logEvent.EventId, mapped.EventId);
        Assert.Equal(logEvent.EventName, mapped.EventName);
        Assert.Equal(logEvent.Message, mapped.Message);
        Assert.Equal(logEvent.MessageTemplate, mapped.MessageTemplate);
        Assert.Equal(logEvent.Exception.Type, mapped.Exception!.Type);
        Assert.Equal(logEvent.Exception.Message, mapped.Exception.Message);
        Assert.Equal(logEvent.Scopes, mapped.Scopes);
        Assert.Equal(logEvent.Properties, mapped.Properties);
        Assert.Equal(logEvent.TraceId, mapped.TraceId);
        Assert.Equal(logEvent.SpanId, mapped.SpanId);
        Assert.Equal(logEvent.CorrelationId, mapped.CorrelationId);
        Assert.Equal(logEvent.TenantId, mapped.TenantId);
        Assert.Equal(logEvent.WorkflowDefinitionId, mapped.WorkflowDefinitionId);
        Assert.Equal(logEvent.WorkflowInstanceId, mapped.WorkflowInstanceId);
        Assert.Equal(logEvent.SourceId, mapped.SourceId);
    }

    [Fact]
    public void MapReader_TreatsNullAndWhitespaceJsonAsEmptyValues()
    {
        var record = new RelationalStructuredLogRecord
        {
            Id = "event-a",
            Sequence = 123,
            Timestamp = RelationalStructuredLogMapper.FormatTimestamp(DateTimeOffset.UtcNow),
            ReceivedAt = RelationalStructuredLogMapper.FormatTimestamp(DateTimeOffset.UtcNow),
            Level = StructuredLogLevel.Information,
            Category = "Elsa.Tests",
            EventId = 42,
            EventName = null,
            Message = "Message",
            MessageTemplate = null,
            ExceptionJson = null,
            ScopesJson = " ",
            PropertiesJson = "",
            TraceId = null,
            SpanId = null,
            CorrelationId = null,
            TenantId = null,
            WorkflowDefinitionId = null,
            WorkflowInstanceId = null,
            SourceId = "source-a"
        };

        using var reader = CreateReader(record);
        Assert.True(reader.Read());

        var mapped = _mapper.Map(reader);

        Assert.Null(mapped.EventName);
        Assert.Null(mapped.MessageTemplate);
        Assert.Null(mapped.Exception);
        Assert.Empty(mapped.Scopes);
        Assert.Empty(mapped.Properties);
        Assert.Null(mapped.TraceId);
        Assert.Null(mapped.SpanId);
        Assert.Null(mapped.CorrelationId);
        Assert.Null(mapped.TenantId);
        Assert.Null(mapped.WorkflowDefinitionId);
        Assert.Null(mapped.WorkflowInstanceId);
    }

    private static DataTableReader CreateReader(RelationalStructuredLogRecord record)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(string));
        table.Columns.Add("Sequence", typeof(long));
        table.Columns.Add("Timestamp", typeof(string));
        table.Columns.Add("ReceivedAt", typeof(string));
        table.Columns.Add("Level", typeof(int));
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("EventId", typeof(int));
        table.Columns.Add("EventName", typeof(string));
        table.Columns.Add("Message", typeof(string));
        table.Columns.Add("MessageTemplate", typeof(string));
        table.Columns.Add("ExceptionJson", typeof(string));
        table.Columns.Add("ScopesJson", typeof(string));
        table.Columns.Add("PropertiesJson", typeof(string));
        table.Columns.Add("TraceId", typeof(string));
        table.Columns.Add("SpanId", typeof(string));
        table.Columns.Add("CorrelationId", typeof(string));
        table.Columns.Add("TenantId", typeof(string));
        table.Columns.Add("WorkflowDefinitionId", typeof(string));
        table.Columns.Add("WorkflowInstanceId", typeof(string));
        table.Columns.Add("SourceId", typeof(string));

        table.Rows.Add(
            record.Id,
            record.Sequence,
            record.Timestamp,
            record.ReceivedAt,
            (int)record.Level,
            record.Category,
            record.EventId,
            record.EventName ?? (object)DBNull.Value,
            record.Message,
            record.MessageTemplate ?? (object)DBNull.Value,
            record.ExceptionJson ?? (object)DBNull.Value,
            record.ScopesJson,
            record.PropertiesJson,
            record.TraceId ?? (object)DBNull.Value,
            record.SpanId ?? (object)DBNull.Value,
            record.CorrelationId ?? (object)DBNull.Value,
            record.TenantId ?? (object)DBNull.Value,
            record.WorkflowDefinitionId ?? (object)DBNull.Value,
            record.WorkflowInstanceId ?? (object)DBNull.Value,
            record.SourceId);

        return table.CreateDataReader();
    }
}
