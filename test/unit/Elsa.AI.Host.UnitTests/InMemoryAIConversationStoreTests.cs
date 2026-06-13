using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.UnitTests;

public class InMemoryAIConversationStoreTests
{
    private readonly InMemoryAIConversationStore _store = new();

    [Fact(DisplayName = "Saving a conversation requires an ID")]
    public async Task SaveAsyncRequiresConversationId()
    {
        var conversation = CreateConversation() with { Id = "" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await _store.SaveAsync(conversation));

        Assert.Equal("conversation", exception.ParamName);
    }

    [Fact(DisplayName = "Saving a conversation requires a user ID")]
    public async Task SaveAsyncRequiresUserId()
    {
        var conversation = CreateConversation() with { UserId = "" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await _store.SaveAsync(conversation));

        Assert.Equal("conversation", exception.ParamName);
    }

    [Fact(DisplayName = "Saving an existing conversation preserves tenant ownership")]
    public async Task SaveAsyncPreservesTenantOwnership()
    {
        await _store.SaveAsync(CreateConversation(tenantId: "tenant-a"));
        var overwrite = CreateConversation(tenantId: "tenant-b");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _store.SaveAsync(overwrite));

        Assert.Equal("Cannot overwrite an AI conversation that belongs to another tenant.", exception.Message);
    }

    [Fact(DisplayName = "Saving an existing conversation preserves user ownership")]
    public async Task SaveAsyncPreservesUserOwnership()
    {
        await _store.SaveAsync(CreateConversation(userId: "user-a"));
        var overwrite = CreateConversation(userId: "user-b");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _store.SaveAsync(overwrite));

        Assert.Equal("Cannot overwrite an AI conversation that belongs to another user.", exception.Message);
    }

    [Fact(DisplayName = "Saving an existing conversation allows the same owner")]
    public async Task SaveAsyncAllowsSameOwnerOverwrite()
    {
        await _store.SaveAsync(CreateConversation(status: AIConversationStatus.Active));
        var completed = CreateConversation(status: AIConversationStatus.Completed);

        await _store.SaveAsync(completed);

        var found = await _store.FindAsync(completed.Id);
        Assert.Equal(AIConversationStatus.Completed, found?.Status);
    }

    [Theory(DisplayName = "Ephemeral terminal conversations expire on lookup")]
    [InlineData(AIConversationStatus.Completed)]
    [InlineData(AIConversationStatus.Failed)]
    public async Task FindAsyncExpiresTerminalEphemeralConversations(AIConversationStatus status)
    {
        var conversation = CreateConversation(
            id: $"ephemeral-{status}",
            status: status,
            retentionMode: AIRetentionMode.Ephemeral);
        await _store.SaveAsync(conversation);

        var found = await _store.FindAsync(conversation.Id);

        Assert.Null(found);
    }

    [Fact(DisplayName = "Ephemeral active conversations remain available")]
    public async Task FindAsyncKeepsActiveEphemeralConversations()
    {
        var conversation = CreateConversation(retentionMode: AIRetentionMode.Ephemeral);
        await _store.SaveAsync(conversation);

        var found = await _store.FindAsync(conversation.Id);

        Assert.NotNull(found);
        Assert.Equal(conversation.Id, found.Id);
    }

    [Fact(DisplayName = "Durable terminal conversations remain available")]
    public async Task FindAsyncKeepsDurableTerminalConversations()
    {
        var conversation = CreateConversation(
            status: AIConversationStatus.Completed,
            retentionMode: AIRetentionMode.Durable,
            retentionExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await _store.SaveAsync(conversation);

        var found = await _store.FindAsync(conversation.Id);

        Assert.NotNull(found);
        Assert.Equal(conversation.Id, found.Id);
    }

    [Fact(DisplayName = "Configured conversations expire after retention deadline")]
    public async Task FindAsyncExpiresConfiguredConversationsAfterRetentionDeadline()
    {
        var conversation = CreateConversation(retentionExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await _store.SaveAsync(conversation);

        var found = await _store.FindAsync(conversation.Id);

        Assert.Null(found);
    }

    [Fact(DisplayName = "Saving a conversation prunes expired conversations")]
    public async Task SaveAsyncPrunesExpiredConversations()
    {
        var expired = CreateConversation(
            id: "expired",
            status: AIConversationStatus.Completed,
            retentionMode: AIRetentionMode.Ephemeral);
        await _store.SaveAsync(expired);

        await _store.SaveAsync(CreateConversation(id: "active"));

        var found = await _store.FindAsync(expired.Id);
        Assert.Null(found);
    }

    private static AIConversation CreateConversation(
        string id = "conversation-1",
        string? tenantId = "tenant-1",
        string userId = "user-1",
        AIConversationStatus status = AIConversationStatus.Active,
        AIRetentionMode retentionMode = AIRetentionMode.Configured,
        DateTimeOffset? retentionExpiresAt = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RetentionMode = retentionMode,
            RetentionExpiresAt = retentionExpiresAt
        };
}
