using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAIProposalStoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AIDbContext _dbContext = default!;

    [Fact(DisplayName = "Proposal store persists and reloads proposals")]
    public async Task ProposalStorePersistsAndReloadsProposals()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = "proposal-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            Status = AIProposalStatus.Validated,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            Rationale = "Create a workflow"
        };

        await store.SaveAsync(proposal);
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(proposal.Id, null);

        Assert.NotNull(reloaded);
        Assert.Equal(AIProposalStatus.Validated, reloaded.Status);
        Assert.Equal("Create a workflow", reloaded.Rationale);
    }

    [Fact(DisplayName = "Proposal store persists proposals with generated IDs")]
    public async Task ProposalStorePersistsProposalsWithGeneratedIds()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
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
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = "proposal-tenant",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
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

    [Fact(DisplayName = "Proposal store rejects cross-tenant overwrites")]
    public async Task ProposalStoreRejectsCrossTenantOverwrites()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        await store.SaveAsync(new AIProposal
        {
            Id = "proposal-cross-tenant",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1"
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIProposal
        {
            Id = "proposal-cross-tenant",
            TenantId = "tenant-2",
            ConversationId = "conversation-2",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-2"
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("proposal-cross-tenant", "tenant-1");

        Assert.Equal("Cannot overwrite an AI proposal that belongs to another tenant.", exception.Message);
        Assert.NotNull(original);
        Assert.Equal("conversation-1", original.ConversationId);
        Assert.Equal("user-1", original.CreatedBy);
    }

    [Fact(DisplayName = "Proposal store preserves creation timestamp on update")]
    public async Task ProposalStorePreservesCreationTimestampOnUpdate()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        await store.SaveAsync(new AIProposal
        {
            Id = "proposal-timestamp",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1",
            CreatedAt = createdAt,
            Rationale = "first"
        });

        await store.SaveAsync(new AIProposal
        {
            Id = "proposal-timestamp",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            Status = AIProposalStatus.Validated,
            CreatedBy = "user-1",
            CreatedAt = createdAt.AddMinutes(5),
            Rationale = "second"
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("proposal-timestamp", null);

        Assert.NotNull(reloaded);
        Assert.Equal(createdAt, reloaded.CreatedAt);
        Assert.Equal("second", reloaded.Rationale);
    }

    [Fact(DisplayName = "Proposal store retries concurrent inserts as updates")]
    public async Task ProposalStoreRetriesConcurrentInsertsAsUpdates()
    {
        var options = new DbContextOptionsBuilder<AIDbContext>().UseSqlite(_connection).Options;
        await using var firstContext = new AIDbContext(options);
        await using var secondContext = new AIDbContext(options);
        var firstStore = new EFCoreAIProposalStore(firstContext);
        var secondStore = new EFCoreAIProposalStore(secondContext);
        var firstProposal = new AIProposal
        {
            Id = "proposal-concurrent",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1",
            Rationale = "first"
        };
        var secondProposal = firstProposal with
        {
            Rationale = "second"
        };

        await firstStore.SaveAsync(firstProposal);
        await secondStore.SaveAsync(secondProposal);
        _dbContext.ChangeTracker.Clear();

        var reloaded = await new EFCoreAIProposalStore(_dbContext).FindAsync("proposal-concurrent", "tenant-1");

        Assert.NotNull(reloaded);
        Assert.Equal("second", reloaded.Rationale);
    }

    [Fact(DisplayName = "Proposal store validates required proposal fields before saving")]
    public async Task ProposalStoreValidatesRequiredProposalFieldsBeforeSaving()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Kind = AIProposalKind.WorkflowCreate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(proposal));

        Assert.Equal("proposal", exception.ParamName);
    }

    [Fact(DisplayName = "Proposal store validates proposal creator before saving")]
    public async Task ProposalStoreValidatesProposalCreatorBeforeSaving()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(proposal));

        Assert.Equal("proposal", exception.ParamName);
        Assert.Equal("A proposal creator is required. (Parameter 'proposal')", exception.Message);
    }


    [Fact(DisplayName = "Proposal store reads enum values case-insensitively")]
    public async Task ProposalStoreReadsEnumValuesCaseInsensitively()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = "proposal-2",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            Status = AIProposalStatus.Validated,
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
        Assert.Equal(AIProposalKind.WorkflowCreate, reloaded.Kind);
        Assert.Equal(AIProposalStatus.Validated, reloaded.Status);
    }

    [Fact(DisplayName = "Proposal store falls back when persisted enum values are invalid")]
    public async Task ProposalStoreFallsBackWhenPersistedEnumValuesAreInvalid()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = "proposal-3",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowUpdate,
            Status = AIProposalStatus.Validated,
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
        Assert.Equal(AIProposalKind.WorkflowCreate, reloaded.Kind);
        Assert.Equal(AIProposalStatus.Draft, reloaded.Status);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbContext = new AIDbContext(new DbContextOptionsBuilder<AIDbContext>().UseSqlite(_connection).Options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
