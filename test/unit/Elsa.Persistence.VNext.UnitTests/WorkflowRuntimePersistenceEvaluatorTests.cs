using Elsa.Persistence.VNext.Runtime.Evaluation;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class WorkflowRuntimePersistenceEvaluatorTests
{
    private readonly WorkflowRuntimePersistenceEvaluator _evaluator = new();

    [Test]
    public async Task Evaluator_AdoptsMetadataStoreWithProviderContractTests()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "WorkflowDefinitions",
            WorkflowRuntimePersistenceArea.Management,
            WorkflowRuntimePersistenceWorkload.Metadata,
            RequiresDistributedLocking: false,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: false,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        await Assert.That(decision.Kind).IsEqualTo(WorkflowRuntimePersistenceDecisionKind.AdoptVNext);
        await Assert.That(decision.Reasons.Single()).Contains("Metadata workloads fit").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Evaluator_DefersHotDistributedLookupWithoutBenchmarkEvidence()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "Bookmarks",
            WorkflowRuntimePersistenceArea.Bookmarks,
            WorkflowRuntimePersistenceWorkload.HotLookup,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: false,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        await Assert.That(decision.Kind).IsEqualTo(WorkflowRuntimePersistenceDecisionKind.DeferUntilBenchmarked);
        await Assert.That(decision.Reasons.Single()).Contains("benchmark and lock/concurrency evidence").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Evaluator_AllowsPhysicalizedHotLookupWhenEvidenceExists()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "Triggers",
            WorkflowRuntimePersistenceArea.Triggers,
            WorkflowRuntimePersistenceWorkload.HotLookup,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        await Assert.That(decision.Kind).IsEqualTo(WorkflowRuntimePersistenceDecisionKind.AdoptWithPhysicalization);
        await Assert.That(decision.RequiredEvidence.Single()).Contains("physicalization policy").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Evaluator_KeepsQueueWorkloadSpecialized()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "BookmarkQueue",
            WorkflowRuntimePersistenceArea.Bookmarks,
            WorkflowRuntimePersistenceWorkload.Queue,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        await Assert.That(decision.Kind).IsEqualTo(WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider);
        await Assert.That(decision.Reasons.Single()).Contains("Queue workloads need").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Evaluator_KeepsOrderedLogSpecialized()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "WorkflowExecutionLog",
            WorkflowRuntimePersistenceArea.ExecutionLogs,
            WorkflowRuntimePersistenceWorkload.AppendOnlyLog,
            RequiresDistributedLocking: false,
            RequiresOrderedAppend: true,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        await Assert.That(decision.Kind).IsEqualTo(WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider);
        await Assert.That(decision.Reasons.Single()).Contains("ordered append").WithComparison(StringComparison.CurrentCulture);
    }
}
