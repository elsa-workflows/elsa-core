using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAiProposalStoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AiDbContext _dbContext = default!;

    [Fact(DisplayName = "Proposal store persists and reloads proposals")]
    public async Task ProposalStorePersistsAndReloadsProposals()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            Id = "proposal-1",
            ConversationId = "conversation-1",
            Kind = AiProposalKind.WorkflowCreate,
            Status = AiProposalStatus.Validated,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            Rationale = "Create a workflow"
        };

        await store.SaveAsync(proposal);
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(proposal.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(AiProposalStatus.Validated, reloaded.Status);
        Assert.Equal("Create a workflow", reloaded.Rationale);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = new AiDbContext(new DbContextOptionsBuilder<AiDbContext>().UseSqlite(_connection).Options);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
