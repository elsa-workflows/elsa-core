using Elsa.Diagnostics.StructuredLogs.Models;
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
}
