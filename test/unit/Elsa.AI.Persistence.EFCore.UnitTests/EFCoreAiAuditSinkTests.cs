using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAiAuditSinkTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AiDbContext _dbContext = default!;

    [Fact(DisplayName = "Audit sink persists audit records")]
    public async Task AuditSinkPersistsAuditRecords()
    {
        var sink = new EFCoreAiAuditSink(_dbContext);

        await sink.RecordAsync(new AiAuditEvent
        {
            Id = "audit-1",
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Type = "prompt.submitted",
            Timestamp = DateTimeOffset.UtcNow,
            Summary = "Prompt submitted"
        });

        var record = await _dbContext.AuditRecords.SingleAsync();
        Assert.Equal("prompt.submitted", record.Type);
        Assert.Equal("Prompt submitted", record.Summary);
    }

    [Fact(DisplayName = "Audit sink persists events with generated IDs")]
    public async Task AuditSinkPersistsEventsWithGeneratedIds()
    {
        var sink = new EFCoreAiAuditSink(_dbContext);

        await sink.RecordAsync(new AiAuditEvent
        {
            ActorId = "user-1",
            Type = "prompt.submitted",
            Timestamp = DateTimeOffset.UtcNow
        });

        var record = await _dbContext.AuditRecords.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(record.Id));
    }

    [Fact(DisplayName = "Audit sink batches audit records")]
    public async Task AuditSinkBatchesAuditRecords()
    {
        var sink = new EFCoreAiAuditSink(_dbContext);

        await sink.RecordManyAsync([
            new AiAuditEvent
            {
                Id = "audit-1",
                ActorId = "user-1",
                Type = "tool.invoked",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AiAuditEvent
            {
                Id = "audit-2",
                ActorId = "user-1",
                Type = "tool.completed",
                Timestamp = DateTimeOffset.UtcNow
            }
        ]);

        var types = await _dbContext.AuditRecords.OrderBy(x => x.Id).Select(x => x.Type).ToListAsync();
        Assert.Equal(["tool.invoked", "tool.completed"], types);
    }

    [Fact(DisplayName = "Audit sink keeps earlier records when a later batch record fails")]
    public async Task AuditSinkKeepsEarlierRecordsWhenLaterBatchRecordFails()
    {
        var sink = new EFCoreAiAuditSink(_dbContext);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await sink.RecordManyAsync([
            new AiAuditEvent
            {
                Id = "audit-duplicate",
                ActorId = "user-1",
                Type = "tool.invoked",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AiAuditEvent
            {
                Id = "audit-duplicate",
                ActorId = "user-1",
                Type = "tool.completed",
                Timestamp = DateTimeOffset.UtcNow
            }
        ]));

        var record = await _dbContext.AuditRecords.SingleAsync();
        Assert.Equal("tool.invoked", record.Type);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = new AiDbContext(new DbContextOptionsBuilder<AiDbContext>().UseSqlite(_connection).Options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
