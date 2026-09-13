using System.Text.Json;

namespace Elsa.Workflows.Runtime.UnitTests.Recovery;

/// <summary>
/// Freezes the JSON shape of <see cref="WorkflowInterruptedPayload"/>. Persisted log entries from prior runtime
/// generations MUST remain readable after this type evolves; new fields go through additive evolution only.
/// </summary>
public class WorkflowInterruptedPayloadContractTests
{
    [Test]
    [DisplayName("Payload round-trips through System.Text.Json with all fields preserved")]
    public async Task RoundTripPreservesAllFields()
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

        await Assert.That(deserialized).IsEqualTo(original);
    }

    [Test]
    [DisplayName("Payload tolerates null optional fields")]
    public async Task NullOptionalsTolerated()
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
        var deserialized = await Assert.That(JsonSerializer.Deserialize<WorkflowInterruptedPayload>(json)).IsNotNull();

        await Assert.That(deserialized.LastActivityId).IsNull();
        await Assert.That(deserialized.IngressSourceName).IsNull();
    }

    [Test]
    [DisplayName("Reason discriminator constants are stable")]
    public async Task ReasonConstantsAreStable()
    {
        // These string values are persisted in execution logs; changing them silently breaks historical queries.
        await Assert.That(WorkflowInterruptedPayload.ReasonDeadlineBreach).IsEqualTo("DeadlineBreach");
        await Assert.That(WorkflowInterruptedPayload.ReasonOperatorForce).IsEqualTo("OperatorForce");
        await Assert.That(WorkflowInterruptedPayload.ReasonPersistenceFailure).IsEqualTo("PersistenceFailure");
        await Assert.That(WorkflowInterruptedPayload.WorkflowInterruptedEventName).IsEqualTo("WorkflowInterrupted");
    }

    [Test]
    [DisplayName("Payload JSON includes the canonical field names")]
    public async Task JsonContainsCanonicalFieldNames()
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

        await Assert.That(json).Contains("\"InterruptedAt\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"Reason\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"GenerationId\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"LastActivityId\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"LastActivityNodeId\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"IngressSourceName\"").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(json).Contains("\"BurstDuration\"").WithComparison(StringComparison.CurrentCulture);
    }
}
