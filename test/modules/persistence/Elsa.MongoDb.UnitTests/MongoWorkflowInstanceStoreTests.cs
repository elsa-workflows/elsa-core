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
        if (document.TryGetValue("Id", out var idValue) && idValue.IsString && idValue.AsString == id)
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
        return filter.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry)).ToBsonDocument();
    }

    private static string Render(UpdateDefinition<WorkflowInstance> update)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        var rendered = update.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
        return rendered.ToJson();
    }
}
