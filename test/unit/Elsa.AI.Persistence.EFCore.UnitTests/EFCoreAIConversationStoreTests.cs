using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Services;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAIConversationStoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AIDbContext _dbContext = default!;

    [Fact(DisplayName = "Conversation store persists and reloads conversations")]
    public async Task ConversationStorePersistsAndReloadsConversations()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var conversation = new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-1",
            Status = AIConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTimeOffset.UtcNow,
            ProviderSessionId = "provider-session-1",
            RetentionMode = AIRetentionMode.Configured,
            RetentionExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Messages =
            [
                new AIMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AIMessageRole.User,
                    Content = "Build a workflow",
                    CreatedAt = DateTimeOffset.UtcNow,
                    StreamSequence = 1,
                    Metadata = new JsonObject { ["source"] = "chat" }
                }
            ]
        };

        await store.SaveAsync(conversation);
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync(conversation.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("tenant-1", reloaded.TenantId);
        Assert.Equal("provider-session-1", reloaded.ProviderSessionId);
        var message = Assert.Single(reloaded.Messages);
        Assert.Equal("Build a workflow", message.Content);
        Assert.Equal("chat", message.Metadata["source"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Conversation store updates existing conversations")]
    public async Task ConversationStoreUpdatesExistingConversations()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-2",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            RetentionExpiresAt = now.AddDays(1),
            Messages = [CreateMessage("message-1", "first")]
        });

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-2",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(1),
            RetentionExpiresAt = now.AddDays(1),
            Messages = [CreateMessage("message-2", "second")]
        });

        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-2");

        Assert.NotNull(reloaded);
        Assert.Equal(AIConversationStatus.Completed, reloaded.Status);
        Assert.Equal("second", Assert.Single(reloaded.Messages).Content);
    }

    [Fact(DisplayName = "Conversation store preserves creation and retention timestamps on update")]
    public async Task ConversationStorePreservesCreationAndRetentionTimestampsOnUpdate()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var retentionExpiresAt = DateTimeOffset.UtcNow.AddDays(1);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-timestamps",
            UserId = "user-1",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            RetentionExpiresAt = retentionExpiresAt,
            Messages = [CreateMessage("message-1", "first")]
        });

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-timestamps",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = createdAt.AddMinutes(5),
            UpdatedAt = createdAt.AddMinutes(5),
            RetentionExpiresAt = retentionExpiresAt.AddMinutes(5),
            Messages = [CreateMessage("message-2", "second")]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-timestamps");

        Assert.NotNull(reloaded);
        Assert.Equal(createdAt, reloaded.CreatedAt);
        Assert.Equal(createdAt.AddMinutes(5), reloaded.UpdatedAt);
        Assert.Equal(retentionExpiresAt, reloaded.RetentionExpiresAt);
    }

    [Fact(DisplayName = "Conversation store rejects cross-tenant overwrites")]
    public async Task ConversationStoreRejectsCrossTenantOverwrites()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-tenant",
            TenantId = "tenant-1",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("message-1", "first")]
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-tenant",
            TenantId = "tenant-2",
            UserId = "user-2",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("message-2", "second")]
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("conversation-cross-tenant");

        Assert.Equal("Cannot overwrite an AI conversation that belongs to another tenant.", exception.Message);
        Assert.NotNull(original);
        Assert.Equal("tenant-1", original.TenantId);
        Assert.Equal("first", Assert.Single(original.Messages).Content);
    }

    [Fact(DisplayName = "Conversation store rejects cross-user overwrites")]
    public async Task ConversationStoreRejectsCrossUserOverwrites()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-user",
            TenantId = "tenant-1",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("message-1", "first")]
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-user",
            TenantId = "tenant-1",
            UserId = "user-2",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("message-2", "second")]
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("conversation-cross-user");

        Assert.Equal("Cannot overwrite an AI conversation that belongs to another user.", exception.Message);
        Assert.NotNull(original);
        Assert.Equal("user-1", original.UserId);
        Assert.Equal("first", Assert.Single(original.Messages).Content);
    }

    [Fact(DisplayName = "Conversation store caps persisted message history")]
    public async Task ConversationStoreCapsPersistedMessageHistory()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var messages = Enumerable
            .Range(0, 300)
            .Select(index => new AIMessage
            {
                Id = $"message-{index}",
                ConversationId = "conversation-capped",
                Role = AIMessageRole.Assistant,
                Content = $"message {index}",
                CreatedAt = DateTimeOffset.UtcNow,
                StreamSequence = index
            })
            .ToList();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-capped",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages = messages
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-capped");

        Assert.NotNull(reloaded);
        Assert.Equal(256, reloaded.Messages.Count);
        Assert.Equal("message-44", reloaded.Messages.First().Id);
    }

    [Fact(DisplayName = "Conversation store truncates oversized message content without splitting surrogate pairs")]
    public async Task ConversationStoreTruncatesOversizedMessageContentWithoutSplittingSurrogatePairs()
    {
        var store = new EFCoreAIConversationStore(_dbContext);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-large-emoji",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-emoji",
                    ConversationId = "conversation-large-emoji",
                    Role = AIMessageRole.Assistant,
                    Content = "x" + string.Concat(Enumerable.Repeat("😀", 600_000)),
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-large-emoji");
        var message = Assert.Single(reloaded!.Messages);

        Assert.True(message.Metadata["truncated"]!.GetValue<bool>());
        Assert.False(char.IsHighSurrogate(message.Content[^1]));
    }

    [Fact(DisplayName = "Conversation store prunes completed ephemeral conversations on read")]
    public async Task ConversationStorePrunesCompletedEphemeralConversationsOnRead()
    {
        var store = new EFCoreAIConversationStore(_dbContext);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-ephemeral",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RetentionMode = AIRetentionMode.Ephemeral
        });

        var reloaded = await store.FindAsync("conversation-ephemeral");

        Assert.Null(reloaded);
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-ephemeral"));
    }

    [Fact(DisplayName = "Conversation store prunes expired configured conversations on read")]
    public async Task ConversationStorePrunesExpiredConfiguredConversationsOnRead()
    {
        var store = new EFCoreAIConversationStore(_dbContext);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-expired",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RetentionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-expired");

        Assert.Null(reloaded);
        Assert.Empty(_dbContext.ChangeTracker.Entries());
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-expired"));
    }

    [Fact(DisplayName = "Conversation store treats configured conversations without expiry as retained")]
    public async Task ConversationStoreTreatsConfiguredConversationsWithoutExpiryAsRetained()
    {
        var store = new EFCoreAIConversationStore(_dbContext);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-no-expiry",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RetentionMode = AIRetentionMode.Configured
        });

        var reloaded = await store.FindAsync("conversation-no-expiry");

        Assert.NotNull(reloaded);
    }

    [Fact(DisplayName = "Conversation cleanup deletes expired persisted conversations")]
    public async Task ConversationCleanupDeletesExpiredPersistedConversations()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new EFCoreAIConversationStore(_dbContext);
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-completed-ephemeral",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now,
            RetentionMode = AIRetentionMode.Ephemeral
        });
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-expired-configured",
            UserId = "user-1",
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-1),
            RetentionMode = AIRetentionMode.Configured,
            RetentionExpiresAt = now.AddMinutes(-1)
        });
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-active",
            UserId = "user-1",
            Status = AIConversationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            RetentionMode = AIRetentionMode.Ephemeral
        });

        var deletedCount = await EFCoreAIConversationCleanup.DeleteExpiredAsync(_dbContext, now);

        Assert.Equal(2, deletedCount);
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-completed-ephemeral"));
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-expired-configured"));
        Assert.True(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-active"));
    }

    [Fact(DisplayName = "Conversation store validates required conversation fields")]
    public async Task ConversationStoreValidatesRequiredConversationFields()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var conversation = new AIConversation
        {
            Id = "conversation-invalid"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(conversation));

        Assert.Equal("conversation", exception.ParamName);
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

    private static AIMessage CreateMessage(string id, string content) =>
        new()
        {
            Id = id,
            ConversationId = "conversation-2",
            Role = AIMessageRole.Assistant,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
