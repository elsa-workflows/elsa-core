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

        var reloaded = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(reloaded);
        Assert.Equal(AiProposalStatus.Validated, reloaded.Status);
        Assert.Equal("Create a workflow", reloaded.Rationale);
    }

    [Fact(DisplayName = "Proposal store persists proposals with generated IDs")]
    public async Task ProposalStorePersistsProposalsWithGeneratedIds()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            ConversationId = "conversation-1",
            Kind = AiProposalKind.WorkflowCreate,
            CreatedBy = "user-1"
        };

        await store.SaveAsync(proposal);
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(reloaded);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.Id));
        Assert.NotEqual(default, reloaded.CreatedAt);
    }

    [Fact(DisplayName = "Proposal store scopes reads by tenant")]
    public async Task ProposalStoreScopesReadsByTenant()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            Id = "proposal-tenant",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AiProposalKind.WorkflowCreate,
            CreatedBy = "user-1"
        };

        await store.SaveAsync(proposal);
        _dbContext.ChangeTracker.Clear();

        var allowed = await store.FindAsync(proposal.Id, "tenant-1");
        var denied = await store.FindAsync(proposal.Id, "tenant-2");
        var host = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(allowed);
        Assert.Null(denied);
        Assert.Null(host);
    }

    [Fact(DisplayName = "Proposal store validates required proposal fields before saving")]
    public async Task ProposalStoreValidatesRequiredProposalFieldsBeforeSaving()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            Kind = AiProposalKind.WorkflowCreate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(proposal));

        Assert.Equal("proposal", exception.ParamName);
    }


    [Fact(DisplayName = "Proposal store reads enum values case-insensitively")]
    public async Task ProposalStoreReadsEnumValuesCaseInsensitively()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            Id = "proposal-2",
            ConversationId = "conversation-1",
            Kind = AiProposalKind.WorkflowCreate,
            Status = AiProposalStatus.Validated,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await store.SaveAsync(proposal);
        var record = await _dbContext.Proposals.FindAsync(proposal.Id);
        record!.Kind = "workflowcreate";
        record.Status = "validated";
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(reloaded);
        Assert.Equal(AiProposalKind.WorkflowCreate, reloaded.Kind);
        Assert.Equal(AiProposalStatus.Validated, reloaded.Status);
    }

    [Fact(DisplayName = "Proposal store falls back when persisted enum values are invalid")]
    public async Task ProposalStoreFallsBackWhenPersistedEnumValuesAreInvalid()
    {
        var store = new EFCoreAiProposalStore(_dbContext);
        var proposal = new AiProposal
        {
            Id = "proposal-3",
            ConversationId = "conversation-1",
            Kind = AiProposalKind.WorkflowUpdate,
            Status = AiProposalStatus.Validated,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await store.SaveAsync(proposal);
        var record = await _dbContext.Proposals.FindAsync(proposal.Id);
        record!.Kind = "renamed-kind";
        record.Status = "renamed-status";
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(reloaded);
        Assert.Equal(AiProposalKind.WorkflowCreate, reloaded.Kind);
        Assert.Equal(AiProposalStatus.Draft, reloaded.Status);
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
