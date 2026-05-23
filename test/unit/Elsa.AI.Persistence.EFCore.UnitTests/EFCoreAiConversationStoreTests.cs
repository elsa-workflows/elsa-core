using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class EFCoreAiConversationStoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private AiDbContext _dbContext = default!;

    [Fact(DisplayName = "Conversation store persists and reloads conversations")]
    public async Task ConversationStorePersistsAndReloadsConversations()
    {
        var store = new EFCoreAiConversationStore(_dbContext);
        var conversation = new AiConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-1",
            Status = AiConversationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTimeOffset.UtcNow,
            ProviderSessionId = "provider-session-1",
            RetentionMode = AiRetentionMode.Configured,
            RetentionExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Messages =
            [
                new AiMessage
                {
                    Id = "message-1",
                    ConversationId = "conversation-1",
                    Role = AiMessageRole.User,
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
        var store = new EFCoreAiConversationStore(_dbContext);
        var now = DateTimeOffset.UtcNow;

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-2",
            UserId = "user-1",
            CreatedAt = now,
            UpdatedAt = now,
            RetentionExpiresAt = now.AddDays(1),
            Messages = [CreateMessage("message-1", "first")]
        });

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-2",
            UserId = "user-1",
            Status = AiConversationStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now.AddMinutes(1),
            RetentionExpiresAt = now.AddDays(1),
            Messages = [CreateMessage("message-2", "second")]
        });

        _dbContext.ChangeTracker.Clear();

        var reloaded = await store.FindAsync("conversation-2");

        Assert.NotNull(reloaded);
        Assert.Equal(AiConversationStatus.Completed, reloaded.Status);
        Assert.Equal("second", Assert.Single(reloaded.Messages).Content);
    }

    [Fact(DisplayName = "Conversation store prunes completed ephemeral conversations on read")]
    public async Task ConversationStorePrunesCompletedEphemeralConversationsOnRead()
    {
        var store = new EFCoreAiConversationStore(_dbContext);

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-ephemeral",
            UserId = "user-1",
            Status = AiConversationStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RetentionMode = AiRetentionMode.Ephemeral
        });

        var reloaded = await store.FindAsync("conversation-ephemeral");

        Assert.Null(reloaded);
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-ephemeral"));
    }

    [Fact(DisplayName = "Conversation store prunes expired configured conversations on read")]
    public async Task ConversationStorePrunesExpiredConfiguredConversationsOnRead()
    {
        var store = new EFCoreAiConversationStore(_dbContext);

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-expired",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RetentionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        var reloaded = await store.FindAsync("conversation-expired");

        Assert.Null(reloaded);
        Assert.False(await _dbContext.Conversations.AnyAsync(x => x.Id == "conversation-expired"));
    }

    [Fact(DisplayName = "Conversation store validates required conversation fields")]
    public async Task ConversationStoreValidatesRequiredConversationFields()
    {
        var store = new EFCoreAiConversationStore(_dbContext);
        var conversation = new AiConversation
        {
            Id = "conversation-invalid"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveAsync(conversation));

        Assert.Equal("conversation", exception.ParamName);
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

    private static AiMessage CreateMessage(string id, string content) =>
        new()
        {
            Id = id,
            ConversationId = "conversation-2",
            Role = AiMessageRole.Assistant,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
