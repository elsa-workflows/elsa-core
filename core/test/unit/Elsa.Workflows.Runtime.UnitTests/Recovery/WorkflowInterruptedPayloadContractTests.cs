using System.Text.Json;

namespace Elsa.Workflows.Runtime.UnitTests.Recovery;

/// <summary>
/// Freezes the JSON shape of <see cref="WorkflowInterruptedPayload"/>. Persisted log entries from prior runtime
/// generations MUST remain readable after this type evolves; new fields go through additive evolution only.
/// </summary>
public class WorkflowInterruptedPayloadContractTests
{
    [Fact(DisplayName = "Payload round-trips through System.Text.Json with all fields preserved")]
    public void RoundTripPreservesAllFields()
    {
        var original = new WorkflowInterruptedPayload(
            InterruptedAt: DateTimeOffset.Parse("2026-04-25T09:30:15Z"),
            Reason: WorkflowInterruptedPayload.ReasonDeadlineBreach,
            GenerationId: "gen-abc-123",
            LastActivityId: "activity-1",
            LastActivityNodeId: "node-2",
            IngressSourceName: "http.trigger",
            ExecutionCycleDuration: TimeSpan.FromSeconds(7));

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkflowInterruptedPayload>(json);

        Assert.Equal(original, deserialized);
    }

    [Fact(DisplayName = "Payload tolerates null optional fields")]
    public void NullOptionalsTolerated()
    {
        var original = new WorkflowInterruptedPayload(
            InterruptedAt: DateTimeOffset.Parse("2026-04-25T09:30:15Z"),
            Reason: WorkflowInterruptedPayload.ReasonOperatorForce,
            GenerationId: "gen-1",
            LastActivityId: null,
            LastActivityNodeId: null,
            IngressSourceName: null,
            ExecutionCycleDuration: TimeSpan.FromMilliseconds(50));

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkflowInterruptedPayload>(json);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.LastActivityId);
        Assert.Null(deserialized.IngressSourceName);
    }

    [Fact(DisplayName = "Reason discriminator constants are stable")]
    public void ReasonConstantsAreStable()
    {
        // These string values are persisted in execution logs; changing them silently breaks historical queries.
        Assert.Equal("DeadlineBreach", WorkflowInterruptedPayload.ReasonDeadlineBreach);
        Assert.Equal("OperatorForce", WorkflowInterruptedPayload.ReasonOperatorForce);
        Assert.Equal("PersistenceFailure", WorkflowInterruptedPayload.ReasonPersistenceFailure);
        Assert.Equal("WorkflowInterrupted", WorkflowInterruptedPayload.WorkflowInterruptedEventName);
    }

    [Fact(DisplayName = "Payload JSON includes the canonical field names")]
    public void JsonContainsCanonicalFieldNames()
    {
        var payload = new WorkflowInterruptedPayload(
            InterruptedAt: DateTimeOffset.UtcNow,
            Reason: "DeadlineBreach",
            GenerationId: "g",
            LastActivityId: "a",
            LastActivityNodeId: "n",
            IngressSourceName: "src",
            ExecutionCycleDuration: TimeSpan.FromSeconds(1));

        var json = JsonSerializer.Serialize(payload);

        Assert.Contains("\"InterruptedAt\"", json);
        Assert.Contains("\"Reason\"", json);
        Assert.Contains("\"GenerationId\"", json);
        Assert.Contains("\"LastActivityId\"", json);
        Assert.Contains("\"LastActivityNodeId\"", json);
        Assert.Contains("\"IngressSourceName\"", json);
        Assert.Contains("\"BurstDuration\"", json);
    }
}
