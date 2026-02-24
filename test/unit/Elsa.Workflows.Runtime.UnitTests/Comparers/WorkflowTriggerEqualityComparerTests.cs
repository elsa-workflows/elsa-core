using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Comparers;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.UnitTests.Comparers;

/// <summary>
/// Regression tests for the payload serialization mismatch between
/// <see cref="WorkflowTriggerEqualityComparer"/> and IPayloadSerializer.
/// 
/// Before the fix, the comparer used PascalCase (default) while IPayloadSerializer used camelCase.
/// This caused the trigger diff to always report existing triggers as "removed" and freshly
/// computed triggers as "added", even when they represented the same logical trigger.
/// The resulting delete-then-reinsert churn on every indexing call created a window for
/// unique constraint violations in multi-pod environments.
/// 
/// The fix ensures the comparer uses the same camelCase PropertyNamingPolicy and normalizes
/// payloads to canonical JSON strings before comparison.
/// </summary>
public class WorkflowTriggerEqualityComparerTests
{
    /// <summary>
    /// A simple payload class that mimics real trigger payloads like HttpEndpointBookmarkPayload.
    /// </summary>
    private record TestPayload(string Path, string Method);
    
    [Fact(DisplayName = "Fresh and round-tripped triggers with identical logical content should be considered equal")]
    public void FreshAndRoundTrippedTriggers_ShouldBeEqual()
    {
        // Arrange: a freshly computed trigger (as produced by TriggerIndexer.CreateWorkflowTriggersAsync)
        var freshPayload = new TestPayload("/api/test", "GET");
        var freshTrigger = CreateTrigger("trigger-1", freshPayload);

        // Arrange: the same trigger after a DB round-trip (save via IPayloadSerializer, then load)
        var roundTrippedPayload = SimulatePayloadRoundTrip(freshPayload);
        var loadedTrigger = CreateTrigger("trigger-1", roundTrippedPayload);

        var comparer = new WorkflowTriggerEqualityComparer();

        // Act
        var areEqual = comparer.Equals(freshTrigger, loadedTrigger);

        // Assert: these should be equal since they represent the same logical trigger.
        // Before the fix, they were NOT equal because:
        // - freshPayload serialized as: {"Path":"/api/test","Method":"GET"} (PascalCase)
        // - roundTrippedPayload (a JsonElement) serialized as: {"path":"/api/test","method":"GET"} (camelCase preserved from DB)
        Assert.True(areEqual,
            "Fresh trigger and DB-round-tripped trigger should be considered equal. " +
            "The payload serialization mismatch between WorkflowTriggerEqualityComparer (PascalCase) " +
            "and IPayloadSerializer (camelCase) causes them to differ.");
    }

    [Fact(DisplayName = "Diff should produce empty Added/Removed sets when comparing fresh triggers against their round-tripped equivalents")]
    public void Diff_FreshVsRoundTripped_ShouldProduceNoDifferences()
    {
        // Arrange: simulate Pod A having indexed triggers (now in DB, round-tripped)
        var payload = new TestPayload("/api/orders", "POST");
        var roundTrippedPayload = SimulatePayloadRoundTrip(payload);

        var existingTrigger = CreateTrigger("existing-id-from-pod-a", roundTrippedPayload, hash: "hash-1");

        // Arrange: Pod B freshly computes the same trigger (as TriggerIndexer would)
        var freshTrigger = CreateTrigger("new-id-from-pod-b", payload, hash: "hash-1"); // Different ID, same logical trigger

        var currentTriggers = new List<StoredTrigger> { existingTrigger };
        var newTriggers = new List<StoredTrigger> { freshTrigger };

        // Act: this is exactly what TriggerIndexer.IndexTriggersInternalAsync does
        var diff = Diff.For(currentTriggers, newTriggers, new WorkflowTriggerEqualityComparer());

        // Assert: the diff should find no changes.
        // Before the fix, it reported Removed=[existingTrigger] and Added=[freshTrigger]
        // because the payload JSON representations differed (camelCase vs PascalCase).
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Single(diff.Unchanged);
    }

    [Fact(DisplayName = "Documents the underlying System.Text.Json casing behavior that necessitated the fix")]
    public void PayloadSerializationMismatch_ProducesDifferentJson()
    {
        // This test documents the raw System.Text.Json behavior difference.
        // Without PropertyNamingPolicy = CamelCase + payload normalization, CLR objects
        // and JsonElement values produce different JSON, which was the root cause.
        var payload = new TestPayload("/api/test", "GET");

        // What WorkflowTriggerEqualityComparer produces for a fresh payload (PascalCase, no naming policy):
        var freshJson = JsonSerializer.Serialize(payload, ComparerOptions);
        // Result: {"Path":"/api/test","Method":"GET"}

        // What IPayloadSerializer stores in the DB (camelCase):
        var storedJson = JsonSerializer.Serialize(payload, PayloadSerializerOptions);
        // Result: {"path":"/api/test","method":"GET"}

        // After loading from DB, Deserialize<object> returns a JsonElement
        var roundTrippedPayload = JsonSerializer.Deserialize<object>(storedJson, PayloadSerializerOptions);

        // When WorkflowTriggerEqualityComparer serializes the round-tripped payload:
        var roundTrippedJson = JsonSerializer.Serialize(roundTrippedPayload, ComparerOptions);
        // Result: {"path":"/api/test","method":"GET"} — camelCase preserved from JsonElement!

        // The mismatch:
        Assert.NotEqual(freshJson, roundTrippedJson); // This proves the bug exists
        Assert.Equal("{\"Path\":\"/api/test\",\"Method\":\"GET\"}", freshJson);
        Assert.Equal("{\"path\":\"/api/test\",\"method\":\"GET\"}", roundTrippedJson);
    }
    
    /// <summary>
    /// IPayloadSerializer options: camelCase with case-insensitive deserialization.
    /// </summary>
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Comparer options used by WorkflowTriggerEqualityComparer (without naming policy).
    /// </summary>
    private static readonly JsonSerializerOptions ComparerOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Simulates the IPayloadSerializer round-trip: serialize with camelCase, then deserialize to object.
    /// This is what happens in TriggerStore.OnSaveAsync -> DB -> TriggerStore.OnLoadAsync.
    /// </summary>
    private static object SimulatePayloadRoundTrip(object payload)
    {
        // Serialize (OnSaveAsync)
        var json = JsonSerializer.Serialize(payload, PayloadSerializerOptions);

        // Deserialize back to object (OnLoadAsync calls Deserialize<object>)
        // System.Text.Json deserializes to JsonElement when target type is object.
        var deserialized = JsonSerializer.Deserialize<object>(json, PayloadSerializerOptions);

        return deserialized!;
    }

    /// <summary>
    /// Creates a StoredTrigger with default values that can be overridden.
    /// </summary>
    private static StoredTrigger CreateTrigger(
        string id,
        object payload,
        string workflowDefinitionId = "workflow-1",
        string workflowDefinitionVersionId = "workflow-1:v1",
        string name = "Elsa.HttpEndpoint",
        string activityId = "activity-1",
        string hash = "some-hash") => new()
    {
        Id = id,
        WorkflowDefinitionId = workflowDefinitionId,
        WorkflowDefinitionVersionId = workflowDefinitionVersionId,
        Name = name,
        ActivityId = activityId,
        Hash = hash,
        Payload = payload
    };
}

