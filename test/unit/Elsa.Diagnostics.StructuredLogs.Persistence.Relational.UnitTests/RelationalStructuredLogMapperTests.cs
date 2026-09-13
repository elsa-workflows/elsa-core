using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Models;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests;

public class RelationalStructuredLogMapperTests
{
    private readonly RelationalStructuredLogMapper _mapper = new();

    [Test]
    public async Task Map_SerializesJsonFields()
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

        await Assert.That(record.ExceptionJson).Contains("Boom").WithComparison(StringComparison.Ordinal);
        await Assert.That(record.ScopesJson).Contains("Scope").WithComparison(StringComparison.Ordinal);
        await Assert.That(record.PropertiesJson).Contains("Property").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task Map_FromReader_DeserializesPersistedFields()
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
        await Assert.That(reader.Read()).IsTrue();
        var mapped = _mapper.Map(reader);

        await Assert.That(mapped.Id).IsEqualTo(logEvent.Id);
        await Assert.That(mapped.Sequence).IsEqualTo(logEvent.Sequence);
        await Assert.That(mapped.Timestamp).IsEqualTo(logEvent.Timestamp.ToUniversalTime());
        await Assert.That(mapped.ReceivedAt).IsEqualTo(logEvent.ReceivedAt.ToUniversalTime());
        await Assert.That(mapped.Level).IsEqualTo(logEvent.Level);
        await Assert.That(mapped.Category).IsEqualTo(logEvent.Category);
        await Assert.That(mapped.EventId).IsEqualTo(logEvent.EventId);
        await Assert.That(mapped.EventName).IsEqualTo(logEvent.EventName);
        await Assert.That(mapped.Message).IsEqualTo(logEvent.Message);
        await Assert.That(mapped.MessageTemplate).IsEqualTo(logEvent.MessageTemplate);
        await Assert.That(mapped.Exception).IsEqualTo(logEvent.Exception);
        await Assert.That(mapped.Scopes["Scope"]).IsEqualTo("Value");
        await Assert.That(mapped.Properties.TryGetValue("Property", out var propertyValue)).IsTrue();
        await Assert.That(propertyValue).IsNull();
        await Assert.That(mapped.TraceId).IsEqualTo(logEvent.TraceId);
        await Assert.That(mapped.SpanId).IsEqualTo(logEvent.SpanId);
        await Assert.That(mapped.CorrelationId).IsEqualTo(logEvent.CorrelationId);
        await Assert.That(mapped.TenantId).IsEqualTo(logEvent.TenantId);
        await Assert.That(mapped.WorkflowDefinitionId).IsEqualTo(logEvent.WorkflowDefinitionId);
        await Assert.That(mapped.WorkflowInstanceId).IsEqualTo(logEvent.WorkflowInstanceId);
        await Assert.That(mapped.SourceId).IsEqualTo(logEvent.SourceId);
    }

    [Test]
    public async Task Map_FromReader_ReturnsEmptyCollections_WhenJsonFieldsAreNull()
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

        using var reader = CreateReader(record, useNullJsonPayloads: true);
        await Assert.That(reader.Read()).IsTrue();
        var mapped = _mapper.Map(reader);

        await Assert.That(mapped.Exception).IsNull();
        await Assert.That(mapped.Scopes).IsEmpty();
        await Assert.That(mapped.Properties).IsEmpty();
        await Assert.That(mapped.EventName).IsNull();
        await Assert.That(mapped.MessageTemplate).IsNull();
    }

    [Test]
    public async Task FormatTimestamp_StoresUtcIso8601Text()
    {
        var timestamp = new DateTimeOffset(2026, 5, 13, 15, 0, 0, TimeSpan.FromHours(2));

        var formatted = RelationalStructuredLogMapper.FormatTimestamp(timestamp);

        await Assert.That(formatted).IsEqualTo("2026-05-13T13:00:00.0000000+00:00");
        await Assert.That(RelationalStructuredLogMapper.ParseTimestamp(formatted)).IsEqualTo(timestamp.ToUniversalTime());
    }

    [Test]
    public async Task Map_FromReader_TreatsWhitespaceJsonAsEmptyValues()
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
            ExceptionJson = " ",
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
        await Assert.That(reader.Read()).IsTrue();

        var mapped = _mapper.Map(reader);

        await Assert.That(mapped.EventName).IsNull();
        await Assert.That(mapped.MessageTemplate).IsNull();
        await Assert.That(mapped.Exception).IsNull();
        await Assert.That(mapped.Scopes).IsEmpty();
        await Assert.That(mapped.Properties).IsEmpty();
        await Assert.That(mapped.TraceId).IsNull();
        await Assert.That(mapped.SpanId).IsNull();
        await Assert.That(mapped.CorrelationId).IsNull();
        await Assert.That(mapped.TenantId).IsNull();
        await Assert.That(mapped.WorkflowDefinitionId).IsNull();
        await Assert.That(mapped.WorkflowInstanceId).IsNull();
    }

    private static DbDataReader CreateReader(RelationalStructuredLogRecord record, bool useNullJsonPayloads = false)
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
            JsonValue(record.ExceptionJson, useNullJsonPayloads),
            JsonValue(record.ScopesJson, useNullJsonPayloads),
            JsonValue(record.PropertiesJson, useNullJsonPayloads),
            record.TraceId ?? (object)DBNull.Value,
            record.SpanId ?? (object)DBNull.Value,
            record.CorrelationId ?? (object)DBNull.Value,
            record.TenantId ?? (object)DBNull.Value,
            record.WorkflowDefinitionId ?? (object)DBNull.Value,
            record.WorkflowInstanceId ?? (object)DBNull.Value,
            record.SourceId);

        return new DisposingDataReader(table, table.CreateDataReader());
    }

    private static object JsonValue(string? value, bool useNullJsonPayloads)
    {
        return useNullJsonPayloads ? DBNull.Value : value ?? (object)DBNull.Value;
    }

    private sealed class DisposingDataReader(DataTable table, DataTableReader reader) : DbDataReader
    {
        public override object this[int ordinal] => reader[ordinal];
        public override object this[string name] => reader[name];
        public override int Depth => reader.Depth;
        public override int FieldCount => reader.FieldCount;
        public override bool HasRows => reader.HasRows;
        public override bool IsClosed => reader.IsClosed;
        public override int RecordsAffected => reader.RecordsAffected;
        public override bool GetBoolean(int ordinal) => reader.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => reader.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => reader.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override string GetDataTypeName(int ordinal) => reader.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => reader.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => reader.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => reader.GetDouble(ordinal);
        public override IEnumerator GetEnumerator() => reader.GetEnumerator();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal) => ordinal switch
        {
            1 => typeof(long),
            4 or 6 => typeof(int),
            _ => typeof(string)
        };

        public override float GetFloat(int ordinal) => reader.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => reader.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => reader.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => reader.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => reader.GetInt64(ordinal);
        public override string GetName(int ordinal) => reader.GetName(ordinal);
        public override int GetOrdinal(string name) => reader.GetOrdinal(name);
        public override DataTable? GetSchemaTable() => reader.GetSchemaTable();
        public override string GetString(int ordinal) => reader.GetString(ordinal);
        public override object GetValue(int ordinal) => reader.GetValue(ordinal);
        public override int GetValues(object[] values) => reader.GetValues(values);
        public override bool IsDBNull(int ordinal) => reader.IsDBNull(ordinal);
        public override bool NextResult() => reader.NextResult();
        public override bool Read() => reader.Read();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                reader.Dispose();
                table.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
