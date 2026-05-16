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
    public void Map_FromReader_DeserializesPersistedFields()
    {
        var logEvent = new StructuredLogEvent
        {
            Id = "event-a",
            Sequence = 42,
            Timestamp = new(2026, 5, 13, 15, 0, 0, TimeSpan.FromHours(2)),
            ReceivedAt = new(2026, 5, 13, 13, 0, 1, TimeSpan.Zero),
            Level = StructuredLogLevel.Critical,
            Category = "Elsa.Tests",
            EventId = 17,
            EventName = "Failed",
            Message = "Failed with scope",
            MessageTemplate = "Failed with {Scope}",
            Exception = new("System.InvalidOperationException", "Boom", "Stack"),
            Scopes = new Dictionary<string, string?> { ["Scope"] = "Value" },
            Properties = new Dictionary<string, string?> { ["Property"] = null },
            TraceId = "trace-a",
            SpanId = "span-a",
            CorrelationId = "correlation-a",
            TenantId = "tenant-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            SourceId = "source-a"
        };
        var record = _mapper.Map(logEvent);

        using var reader = CreateReader(record);
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
        Assert.Equal(logEvent.Exception, mapped.Exception);
        Assert.Equal("Value", mapped.Scopes["Scope"]);
        Assert.True(mapped.Properties.ContainsKey("Property"));
        Assert.Null(mapped.Properties["Property"]);
        Assert.Equal(logEvent.TraceId, mapped.TraceId);
        Assert.Equal(logEvent.SpanId, mapped.SpanId);
        Assert.Equal(logEvent.CorrelationId, mapped.CorrelationId);
        Assert.Equal(logEvent.TenantId, mapped.TenantId);
        Assert.Equal(logEvent.WorkflowDefinitionId, mapped.WorkflowDefinitionId);
        Assert.Equal(logEvent.WorkflowInstanceId, mapped.WorkflowInstanceId);
        Assert.Equal(logEvent.SourceId, mapped.SourceId);
    }

    [Fact]
    public void Map_FromReader_ReturnsEmptyCollections_WhenJsonFieldsAreNull()
    {
        var record = new RelationalStructuredLogRecord
        {
            Id = "event-a",
            Sequence = 1,
            Timestamp = RelationalStructuredLogMapper.FormatTimestamp(DateTimeOffset.UtcNow),
            ReceivedAt = RelationalStructuredLogMapper.FormatTimestamp(DateTimeOffset.UtcNow),
            Level = StructuredLogLevel.Information,
            Category = "Elsa.Tests",
            EventId = 0,
            Message = "Hello",
            SourceId = "source-a"
        };

        using var reader = CreateReader(record);
        Assert.True(reader.Read());
        var mapped = _mapper.Map(reader);

        Assert.Null(mapped.Exception);
        Assert.Empty(mapped.Scopes);
        Assert.Empty(mapped.Properties);
        Assert.Null(mapped.EventName);
        Assert.Null(mapped.MessageTemplate);
    }

    [Fact]
    public void FormatTimestamp_StoresUtcIso8601Text()
    {
        var timestamp = new DateTimeOffset(2026, 5, 13, 15, 0, 0, TimeSpan.FromHours(2));

        var formatted = RelationalStructuredLogMapper.FormatTimestamp(timestamp);

        Assert.Equal("2026-05-13T13:00:00.0000000+00:00", formatted);
        Assert.Equal(timestamp.ToUniversalTime(), RelationalStructuredLogMapper.ParseTimestamp(formatted));
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
            record.ScopesJson ?? (object)DBNull.Value,
            record.PropertiesJson ?? (object)DBNull.Value,
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
