using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

    [Fact(DisplayName = "Proposal store treats null and empty tenant IDs as default tenant")]
    public async Task ProposalStoreTreatsNullAndEmptyTenantIdsAsDefaultTenant()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = "proposal-default-tenant",
            TenantId = "",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1"
        };

        await store.SaveAsync(proposal);
        _dbContext.ChangeTracker.Clear();

        var reloadedWithNull = await store.FindAsync(proposal.Id, null);
        var reloadedWithEmpty = await store.FindAsync(proposal.Id, "");

        Assert.NotNull(reloadedWithNull);
        Assert.NotNull(reloadedWithEmpty);
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

    [Fact(DisplayName = "Proposal store rejects cross-user overwrites")]
    public async Task ProposalStoreRejectsCrossUserOverwrites()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        await store.SaveAsync(new AIProposal
        {
            Id = "proposal-cross-user",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1"
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIProposal
        {
            Id = "proposal-cross-user",
            TenantId = "tenant-1",
            ConversationId = "conversation-2",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-2"
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("proposal-cross-user", "tenant-1");

        Assert.Equal("Cannot overwrite an AI proposal that belongs to another user.", exception.Message);
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
        var options = new DbContextOptionsBuilder<AIDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ConcurrentProposalInsertInterceptor(_connection))
            .Options;
        await using var context = new AIDbContext(options);
        var store = new EFCoreAIProposalStore(context);
        var proposal = new AIProposal
        {
            Id = "proposal-concurrent",
            TenantId = "tenant-1",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1",
            Rationale = "second"
        };

        await store.SaveAsync(proposal);
        context.ChangeTracker.Clear();

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

    [Fact(DisplayName = "Proposal store validates proposal ID before saving")]
    public async Task ProposalStoreValidatesProposalIdBeforeSaving()
    {
        var store = new EFCoreAIProposalStore(_dbContext);
        var proposal = new AIProposal
        {
            Id = " ",
            ConversationId = "conversation-1",
            Kind = AIProposalKind.WorkflowCreate,
            CreatedBy = "user-1",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(proposal));

        Assert.Equal("proposal", exception.ParamName);
        Assert.Equal("A proposal ID is required. (Parameter 'proposal')", exception.Message);
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

    private class ConcurrentProposalInsertInterceptor(SqliteConnection connection) : SaveChangesInterceptor
    {
        private bool _inserted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var proposal = eventData.Context?.ChangeTracker
                .Entries<AIProposalRecord>()
                .FirstOrDefault(x => x.State == EntityState.Added)
                ?.Entity;
            if (_inserted || proposal == null)
                return result;

            _inserted = true;
            var options = new DbContextOptionsBuilder<AIDbContext>().UseSqlite(connection).Options;
            await using var competingContext = new AIDbContext(options);
            competingContext.Proposals.Add(new AIProposalRecord
            {
                Id = proposal.Id,
                TenantId = proposal.TenantId,
                ConversationId = proposal.ConversationId,
                Kind = proposal.Kind,
                Status = proposal.Status,
                WorkflowPayload = proposal.WorkflowPayload,
                Rationale = "first",
                Warnings = proposal.Warnings,
                ValidationDiagnostics = proposal.ValidationDiagnostics,
                CreatedBy = proposal.CreatedBy,
                CreatedAt = proposal.CreatedAt
            });
            await competingContext.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}
