using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.MongoDb.UnitTests;

public class MongoWorkflowInstanceStoreTests
{
    [Fact(DisplayName = "TryMarkInterruptedAsync updates Running instances to Running+Interrupted")]
    public async Task TryMarkInterruptedAsync_UpdatesNonTerminalInstance()
    {
        var (store, collection) = CreateStore(matchedCount: 1);

        var marked = await store.TryMarkInterruptedAsync("running-1");

        Assert.True(marked);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterRefusesFinished(filter, "running-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdatePromotesToRunningInterrupted(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Cancelled")]
    public async Task TryMarkInterruptedAsync_FilterRefusesFinishedCancelledByDefault()
    {
        var (store, collection) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("cancelled-1");

        Assert.False(marked);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterRefusesFinished(filter, "cancelled-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdatePromotesToRunningInterrupted(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Cancelled when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_FilterRefusesFinishedCancelledWhenFlagSet()
    {
        var (store, collection) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("cancelled-1", allowFinishedCancelled: true);

        Assert.False(marked);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterRefusesFinished(filter, "cancelled-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdatePromotesToRunningInterrupted(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Finished when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_RefusesFinishedEvenWhenCancelledAllowed()
    {
        var (store, _) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("finished-1", allowFinishedCancelled: true);

        Assert.False(marked);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Faulted when allowFinishedCancelled is true")]
    public async Task TryMarkInterruptedAsync_RefusesFaultedEvenWhenCancelledAllowed()
    {
        var (store, _) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("faulted-1", allowFinishedCancelled: true);

        Assert.False(marked);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync returns false when no interruptible instance matches")]
    public async Task TryMarkInterruptedAsync_ReturnsFalseWhenMissingOrFinished()
    {
        var (store, _) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("finished-1");

        Assert.False(marked);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync sets only SubStatus and UpdatedAt when the row is still the expected running sub-status")]
    public async Task TryTransitionSubStatusAsync_TargetsSubStatusAndUpdatedAt()
    {
        var (store, collection) = CreateStore(matchedCount: 1);
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt);

        Assert.True(marked);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterMarksSuspendedRunning(filter, "wf-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdateSetsCancellingOnly(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync does not write when the expected sub-status is Executing")]
    public async Task TryTransitionSubStatusAsync_DoesNotWriteWhenExpectedSubStatusIsExecuting()
    {
        var (store, collection) = CreateStore(matchedCount: 1);

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Executing, null, WorkflowSubStatus.Cancelling, DateTimeOffset.UtcNow);

        Assert.False(marked);
        await collection.DidNotReceive().UpdateOneAsync(
            Arg.Any<FilterDefinition<WorkflowInstance>>(),
            Arg.Any<UpdateDefinition<WorkflowInstance>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync matches Cancelling and the mark stamp, and writes only the previous sub-status")]
    public async Task TryTransitionSubStatusAsync_MatchesCancellingGeneration()
    {
        var (store, collection) = CreateStore(matchedCount: 0);
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        var restored = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Cancelling, cancellingAt, WorkflowSubStatus.Suspended, cancellingAt.AddMinutes(1));

        Assert.False(restored);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterRestoresCancellingGeneration(filter, "wf-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdateRestoresSuspendedOnly(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static (MongoWorkflowInstanceStore Store, IMongoCollection<WorkflowInstance> Collection) CreateStore(long matchedCount)
    {
        var collection = Substitute.For<IMongoCollection<WorkflowInstance>>();
        var updateResult = Substitute.For<UpdateResult>();
        updateResult.IsAcknowledged.Returns(true);
        updateResult.MatchedCount.Returns(matchedCount);
        updateResult.ModifiedCount.Returns(matchedCount);

        collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<WorkflowInstance>>(),
                Arg.Any<UpdateDefinition<WorkflowInstance>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(updateResult);

        var mongoDbStore = new MongoDbStore<WorkflowInstance>(collection, Substitute.For<ITenantAccessor>());
        var store = new MongoWorkflowInstanceStore(mongoDbStore, Substitute.For<ILogger<MongoWorkflowInstanceStore>>());
        return (store, collection);
    }

    /// <summary>
    /// Default filter: exact Id match and Status != Finished. No store-wide Cancelled OR.
    /// </summary>
    private static bool FilterRefusesFinished(FilterDefinition<WorkflowInstance> filter, string id)
    {
        var document = RenderDocument(filter);
        var rendered = document.ToJson();
        return FilterHasId(document, id)
               && rendered.Contains("Status", StringComparison.Ordinal)
               && rendered.Contains("$ne", StringComparison.Ordinal)
               && rendered.Contains(((int)WorkflowStatus.Finished).ToString(), StringComparison.Ordinal)
               && !rendered.Contains("$or", StringComparison.Ordinal)
               && !rendered.Contains(((int)WorkflowSubStatus.Cancelled).ToString(), StringComparison.Ordinal);
    }

    private static bool FilterHasId(BsonDocument document, string id)
    {
        if (TryGetId(document, out var idValue) && idValue == id)
            return true;

        if (document.TryGetValue("$and", out var andValue) && andValue.IsBsonArray)
        {
            foreach (var item in andValue.AsBsonArray)
            {
                if (item.IsBsonDocument && FilterHasId(item.AsBsonDocument, id))
                    return true;
            }
        }

        return false;
    }

    private static bool TryGetId(BsonDocument document, out string? id)
    {
        foreach (var name in new[] { "_id", "Id" })
        {
            if (document.TryGetValue(name, out var value) && value.IsString)
            {
                id = value.AsString;
                return true;
            }
        }

        id = null;
        return false;
    }

    private static bool UpdatePromotesToRunningInterrupted(UpdateDefinition<WorkflowInstance> update)
    {
        var rendered = Render(update);
        return rendered.Contains("$set", StringComparison.Ordinal)
               && rendered.Contains("Status", StringComparison.Ordinal)
               && rendered.Contains(((int)WorkflowStatus.Running).ToString(), StringComparison.Ordinal)
               && rendered.Contains("SubStatus", StringComparison.Ordinal)
               && rendered.Contains(((int)WorkflowSubStatus.Interrupted).ToString(), StringComparison.Ordinal)
               && rendered.Contains("IsExecuting", StringComparison.Ordinal);
    }

    private static BsonDocument RenderDocument(FilterDefinition<WorkflowInstance> filter)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        return filter.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
    }

    private static string Render(UpdateDefinition<WorkflowInstance> update)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        var rendered = update.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
        return rendered.ToJson();
    }

    private static bool FilterMarksSuspendedRunning(FilterDefinition<WorkflowInstance> filter, string id)
    {
        var document = RenderDocument(filter);
        return FilterHasId(document, id)
               && EqualsField(document, "Status", (int)WorkflowStatus.Running)
               && EqualsField(document, "SubStatus", (int)WorkflowSubStatus.Suspended)
               && ContainsField(document, "IsExecuting");
    }

    private static bool UpdateSetsCancellingOnly(UpdateDefinition<WorkflowInstance> update)
    {
        var set = SetDocument(update);
        return set.ElementCount == 2
               && set["SubStatus"].ToInt32() == (int)WorkflowSubStatus.Cancelling
               && set.Contains("UpdatedAt")
               && !set.Contains("Status")
               && !set.Contains("WorkflowState");
    }

    private static bool FilterRestoresCancellingGeneration(FilterDefinition<WorkflowInstance> filter, string id)
    {
        var document = RenderDocument(filter);
        return FilterHasId(document, id)
               && EqualsField(document, "Status", (int)WorkflowStatus.Running)
               && EqualsField(document, "SubStatus", (int)WorkflowSubStatus.Cancelling)
               && ContainsField(document, "UpdatedAt");
    }

    private static bool UpdateRestoresSuspendedOnly(UpdateDefinition<WorkflowInstance> update)
    {
        var set = SetDocument(update);
        return set.ElementCount == 2
               && set["SubStatus"].ToInt32() == (int)WorkflowSubStatus.Suspended
               && set.Contains("UpdatedAt")
               && !set.Contains("WorkflowState");
    }

    private static BsonDocument SetDocument(UpdateDefinition<WorkflowInstance> update)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        var rendered = update.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
        return rendered["$set"].AsBsonDocument;
    }

    /// <summary>
    /// Matches an equality on <paramref name="fieldName"/>. A <c>$ne</c> clause for the same field does not count.
    /// </summary>
    private static bool EqualsField(BsonValue node, string fieldName, int expected)
    {
        switch (node.BsonType)
        {
            case BsonType.Document:
                foreach (var element in node.AsBsonDocument)
                {
                    if (element.Name == fieldName && IsEqualValue(element.Value, expected))
                        return true;

                    if (element.Name != fieldName && EqualsField(element.Value, fieldName, expected))
                        return true;
                }

                return false;
            case BsonType.Array:
                foreach (var item in node.AsBsonArray)
                {
                    if (EqualsField(item, fieldName, expected))
                        return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool IsEqualValue(BsonValue value, int expected)
    {
        if (value.IsNumeric)
            return value.ToInt32() == expected;

        return value.IsBsonDocument
               && value.AsBsonDocument.TryGetValue("$eq", out var equal)
               && equal.IsNumeric
               && equal.ToInt32() == expected;
    }

    private static bool ContainsField(BsonValue node, string fieldName)
    {
        switch (node.BsonType)
        {
            case BsonType.Document:
                foreach (var element in node.AsBsonDocument)
                {
                    if (element.Name == fieldName || ContainsField(element.Value, fieldName))
                        return true;
                }

                return false;
            case BsonType.Array:
                foreach (var item in node.AsBsonArray)
                {
                    if (ContainsField(item, fieldName))
                        return true;
                }

                return false;
            default:
                return false;
        }
    }
}
