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
