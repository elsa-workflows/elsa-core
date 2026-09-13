using System.Text;
using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Services;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAIConversationStoreTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AIDbContext _dbContext = default!;

    [Test]
    [DisplayName("Conversation store persists and reloads conversations")]
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

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.TenantId).IsEqualTo("tenant-1");
        await Assert.That(reloaded.ProviderSessionId).IsEqualTo("provider-session-1");
        var message = await Assert.That(reloaded.Messages).HasSingleItem();
        await Assert.That(message.Content).IsEqualTo("Build a workflow");
        await Assert.That(message.Metadata["source"]!.GetValue<string>()).IsEqualTo("chat");
    }

    [Test]
    [DisplayName("Conversation store updates existing conversations")]
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
            Messages = [CreateMessage("conversation-2", "message-1", "first")]
        });

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-2",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(1),
            RetentionExpiresAt = now.AddDays(1),
            Messages = [CreateMessage("conversation-2", "message-2", "second")]
        });

        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-2");

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.Status).IsEqualTo(AIConversationStatus.Completed);
        var reloadedMessage = await Assert.That(reloaded.Messages).HasSingleItem();
        await Assert.That(reloadedMessage.Content).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Conversation store preserves creation timestamp and updates retention timestamp")]
    public async Task ConversationStorePreservesCreationTimestampAndUpdatesRetentionTimestamp()
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
            Messages = [CreateMessage("conversation-timestamps", "message-1", "first")]
        });

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-timestamps",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            CreatedAt = createdAt.AddMinutes(5),
            UpdatedAt = createdAt.AddMinutes(5),
            RetentionExpiresAt = retentionExpiresAt.AddMinutes(5),
            Messages = [CreateMessage("conversation-timestamps", "message-2", "second")]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-timestamps");

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(reloaded.UpdatedAt).IsEqualTo(createdAt.AddMinutes(5));
        await Assert.That(reloaded.RetentionExpiresAt).IsEqualTo(retentionExpiresAt.AddMinutes(5));
    }

    [Test]
    [DisplayName("Conversation store rejects cross-tenant overwrites")]
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
            Messages = [CreateMessage("conversation-cross-tenant", "message-1", "first")]
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-tenant",
            TenantId = "tenant-2",
            UserId = "user-2",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("conversation-cross-tenant", "message-2", "second")]
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("conversation-cross-tenant");

        await Assert.That(exception!.Message).IsEqualTo("Cannot overwrite an AI conversation that belongs to another tenant.");
        await Assert.That(original).IsNotNull();
        await Assert.That(original.TenantId).IsEqualTo("tenant-1");
        var originalMessage = await Assert.That(original.Messages).HasSingleItem();
        await Assert.That(originalMessage.Content).IsEqualTo("first");
    }

    [Test]
    [DisplayName("Conversation store treats null and empty tenant IDs as default tenant")]
    public async Task ConversationStoreTreatsNullAndEmptyTenantIdsAsDefaultTenant()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-default-tenant",
            TenantId = null,
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("conversation-default-tenant", "message-1", "first")]
        });
        _dbContext.ChangeTracker.Clear();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-default-tenant",
            TenantId = "",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(1),
            Messages = [CreateMessage("conversation-default-tenant", "message-2", "second")]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-default-tenant");

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.TenantId).IsEqualTo("");
        var reloadedMessage = await Assert.That(reloaded.Messages).HasSingleItem();
        await Assert.That(reloadedMessage.Content).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Conversation store rejects cross-user overwrites")]
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
            Messages = [CreateMessage("conversation-cross-user", "message-1", "first")]
        });
        _dbContext.ChangeTracker.Clear();

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-cross-user",
            TenantId = "tenant-1",
            UserId = "user-2",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = [CreateMessage("conversation-cross-user", "message-2", "second")]
        }));
        _dbContext.ChangeTracker.Clear();

        var original = await store.FindAsync("conversation-cross-user");

        await Assert.That(exception!.Message).IsEqualTo("Cannot overwrite an AI conversation that belongs to another user.");
        await Assert.That(original).IsNotNull();
        await Assert.That(original.UserId).IsEqualTo("user-1");
        var originalMessage = await Assert.That(original.Messages).HasSingleItem();
        await Assert.That(originalMessage.Content).IsEqualTo("first");
    }

    [Test]
    [DisplayName("Conversation store caps persisted message history")]
    public async Task ConversationStoreCapsPersistedMessageHistory()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;
        var messages = Enumerable
            .Range(0, 300)
            .Select(index => new AIMessage
            {
                Id = $"message-{index}",
                ConversationId = "conversation-capped",
                Role = AIMessageRole.Assistant,
                Content = $"message {index}",
                CreatedAt = now.AddSeconds(index),
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

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.Messages.Count).IsEqualTo(256);
        await Assert.That(reloaded.Messages.First().Id).IsEqualTo("message-44");
    }

    [Test]
    [DisplayName("Conversation store caps message history by message chronology")]
    public async Task ConversationStoreCapsMessageHistoryByMessageChronology()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;
        var messages = Enumerable
            .Range(0, 300)
            .Select(index => new AIMessage
            {
                Id = $"message-{index}",
                ConversationId = "conversation-chronology",
                Role = AIMessageRole.Assistant,
                Content = $"message {index}",
                CreatedAt = now.AddSeconds(index),
                StreamSequence = index
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-chronology",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            Messages = messages
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-chronology");

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded.Messages.Count).IsEqualTo(256);
        await Assert.That(reloaded.Messages.First().Id).IsEqualTo("message-44");
        await Assert.That(reloaded.Messages.Last().Id).IsEqualTo("message-299");
    }

    [Test]
    [DisplayName("Conversation store truncates oversized message content without splitting surrogate pairs")]
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
                    Content = "x" + string.Concat(Enumerable.Repeat("😀", 90_000)),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new JsonObject { ["source"] = "chat" }
                }
            ]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-large-emoji");
        var record = await _dbContext.Conversations.AsNoTracking().SingleAsync(x => x.Id == "conversation-large-emoji");
        var message = await Assert.That(reloaded!.Messages).HasSingleItem();

        await Assert.That(Encoding.UTF8.GetByteCount(record.Messages) <= 1024 * 1024).IsTrue();
        await Assert.That(message.Metadata["source"]!.GetValue<string>()).IsEqualTo("chat");
        await Assert.That(message.Metadata["truncated"]!.GetValue<bool>()).IsTrue();
        await Assert.That(char.IsHighSurrogate(message.Content[^1])).IsFalse();
    }

    [Test]
    [DisplayName("Conversation store drops oversized metadata when truncation still exceeds the byte limit")]
    public async Task ConversationStoreDropsOversizedMetadataWhenTruncationStillExceedsTheByteLimit()
    {
        var store = new EFCoreAIConversationStore(_dbContext);

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-large-metadata",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Messages =
            [
                new AIMessage
                {
                    Id = "message-metadata",
                    ConversationId = "conversation-large-metadata",
                    Role = AIMessageRole.Assistant,
                    Content = new string('x', 128),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new JsonObject { ["source"] = new string('m', 2 * 1024 * 1024) }
                }
            ]
        });
        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-large-metadata");
        var record = await _dbContext.Conversations.AsNoTracking().SingleAsync(x => x.Id == "conversation-large-metadata");
        var message = await Assert.That(reloaded!.Messages).HasSingleItem();

        await Assert.That(Encoding.UTF8.GetByteCount(record.Messages) <= 1024 * 1024).IsTrue();
        await Assert.That(message.Metadata["source"]).IsNull();
        await Assert.That(message.Metadata["truncated"]!.GetValue<bool>()).IsTrue();
    }

    [Test]
    [DisplayName("Conversation store hides completed ephemeral conversations on read")]
    public async Task ConversationStoreHidesCompletedEphemeralConversationsOnRead()
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

        await Assert.That(reloaded).IsNull();
        await Assert.That(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-ephemeral")).IsTrue();
    }

    [Test]
    [DisplayName("Conversation store hides expired configured conversations on read")]
    public async Task ConversationStoreHidesExpiredConfiguredConversationsOnRead()
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

        await Assert.That(reloaded).IsNull();
        await Assert.That(_dbContext.ChangeTracker.Entries()).IsEmpty();
        await Assert.That(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-expired")).IsTrue();
    }

    [Test]
    [DisplayName("Conversation store treats configured conversations without expiry as retained")]
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

        await Assert.That(reloaded).IsNotNull();
    }

    [Test]
    [DisplayName("Conversation cleanup deletes expired persisted conversations")]
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

        await Assert.That(deletedCount).IsEqualTo(2);
        await Assert.That(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-completed-ephemeral")).IsFalse();
        await Assert.That(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-expired-configured")).IsFalse();
        await Assert.That(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-active")).IsTrue();
    }

    [Test]
    [DisplayName("Conversation store validates required conversation fields")]
    public async Task ConversationStoreValidatesRequiredConversationFields()
    {
        var store = new EFCoreAIConversationStore(_dbContext);
        var conversation = new AIConversation
        {
            Id = "conversation-invalid"
        };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await store.SaveAsync(conversation));

        await Assert.That(exception!.ParamName).IsEqualTo("conversation");
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

    private static AIMessage CreateMessage(string conversationId, string id, string content) =>
        new()
        {
            Id = id,
            ConversationId = conversationId,
            Role = AIMessageRole.Assistant,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
