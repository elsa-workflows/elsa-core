using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAIAuditSinkTests : IAsyncInitializer, IAsyncDisposable
{
    private static readonly NullLogger<EFCoreAIAuditSink> AuditLogger = NullLogger<EFCoreAIAuditSink>.Instance;
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AIDbContext _dbContext = default!;

    [Test]
    [DisplayName("Audit sink persists audit records")]
    public async Task AuditSinkPersistsAuditRecords()
    {
        var sink = new EFCoreAIAuditSink(_dbContext, AuditLogger);

        await sink.RecordAsync(new AIAuditEvent
        {
            Id = "audit-1",
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Type = "prompt.submitted",
            Timestamp = DateTimeOffset.UtcNow,
            Summary = "Prompt submitted"
        });

        var record = await _dbContext.AuditRecords.SingleAsync();
        await Assert.That(record.Type).IsEqualTo("prompt.submitted");
        await Assert.That(record.Summary).IsEqualTo("Prompt submitted");
    }

    [Test]
    [DisplayName("Audit sink persists events with generated IDs")]
    public async Task AuditSinkPersistsEventsWithGeneratedIds()
    {
        var sink = new EFCoreAIAuditSink(_dbContext, AuditLogger);

        await sink.RecordAsync(new AIAuditEvent
        {
            ActorId = "user-1",
            Type = "prompt.submitted",
            Timestamp = DateTimeOffset.UtcNow
        });

        var record = await _dbContext.AuditRecords.SingleAsync();
        await Assert.That(string.IsNullOrWhiteSpace(record.Id)).IsFalse();
    }

    [Test]
    [DisplayName("Audit sink batches audit records")]
    public async Task AuditSinkBatchesAuditRecords()
    {
        var sink = new EFCoreAIAuditSink(_dbContext, AuditLogger);

        await sink.RecordManyAsync([
            new AIAuditEvent
            {
                Id = "audit-1",
                ActorId = "user-1",
                Type = "tool.invoked",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AIAuditEvent
            {
                Id = "audit-2",
                ActorId = "user-1",
                Type = "tool.completed",
                Timestamp = DateTimeOffset.UtcNow
            }
        ]);

        var types = await _dbContext.AuditRecords.OrderBy(x => x.Id).Select(x => x.Type).ToListAsync();
        await Assert.That(types).IsEquivalentTo(["tool.invoked", "tool.completed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Audit sink rolls back batch records when a batch record fails")]
    public async Task AuditSinkRollsBackBatchRecordsWhenBatchRecordFails()
    {
        var sink = new EFCoreAIAuditSink(_dbContext, AuditLogger);

        await sink.RecordManyAsync([
            new AIAuditEvent
            {
                Id = "audit-duplicate",
                ActorId = "user-1",
                Type = "tool.invoked",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AIAuditEvent
            {
                Id = "audit-duplicate",
                ActorId = "user-1",
                Type = "tool.completed",
                Timestamp = DateTimeOffset.UtcNow
            }
        ]);

        await Assert.That(await _dbContext.AuditRecords.AnyAsync()).IsFalse();
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = new AIDbContext(new DbContextOptionsBuilder<AIDbContext>().UseSqliteAIMigrations(_connection).Options);
        await _dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
